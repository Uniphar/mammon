namespace Mammon.Services;

public class CostManagementService
{
    private readonly ArmClient armClient;
    private readonly HttpClient httpClient;
    private readonly ILogger<CostManagementService> logger;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    private const string costAPIVersion = "2023-11-01";

    private const string costColumnName = "Cost";
    private const string resourceIdColumnName = "ResourceId";
    private const string currencyColumnName = "Currency";

    public CostManagementService(ArmClient armClient, HttpClient httpClient, ILogger<CostManagementService> logger)
    {
        this.armClient = armClient;
        this.httpClient = httpClient;
        this.logger = logger;
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
    }

    public virtual string? GetSubscriptionId(string subscriptionName)
    {
        var subByName = armClient.GetSubscriptions().FirstOrDefault((s) => s.Data.DisplayName == subscriptionName);

        return subByName?.Id ?? string.Empty;
    }

    public async Task<AzureCostResponse> QueryForSubAsync(CostReportRequest request)
    {
        new CostReportRequestValidator().ValidateAndThrow(request);

        var subId = GetSubscriptionId(request.SubscriptionName);
        if (string.IsNullOrWhiteSpace(subId))
            throw new InvalidOperationException($"Unable to find subscription {request.SubscriptionName}");

        var costApirequest = $"{{\"type\":\"ActualCost\",\"dataSet\":{{\"granularity\":\"None\",\"aggregation\":{{\"totalCost\":{{\"name\":\"Cost\",\"function\":\"Sum\"}}}},\"grouping\":[{{\"type\":\"Dimension\",\"name\":\"ResourceId\"}}]}},\"timeframe\":\"Custom\",\"timePeriod\":{{\"from\":\"{request.CostFrom:yyyy-MM-dd}T00:00:00+00:00\",\"to\":\"{request.CostTo:yyyy-MM-dd}T00:00:00+00:00\"}}}}";
   
        var content = new StringContent(costApirequest, Encoding.UTF8, "application/json");

        //TODO: check no granularity support via https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.costmanagement.models.granularitytype.-ctor?view=azure-dotnet#azure-resourcemanager-costmanagement-models-granularitytype-ctor(system-string)

        HttpResponseMessage response;
        string? url = $"https://management.azure.com{subId}/providers/Microsoft.CostManagement/query?api-version={costAPIVersion}";
        bool nextPageAvailable;

        AzureCostResponse responseData = [];

        do
        {
            response = await httpClient.PostAsync(
                url,
            content);

            response.EnsureSuccessStatusCode();

            var (nextLink, costs) = ParseRawJson(await response.Content.ReadAsStringAsync());

            responseData.AddRange(costs);

            nextPageAvailable = !string.IsNullOrWhiteSpace(nextLink);
            url = nextLink;

        }
        while (nextPageAvailable);

        return responseData;
    }

    private (string? nextLink, List<ResourceCost> costs) ParseRawJson(string content)
    {
        //ugly workaround to deal with invalid cost api response, alternatives are to write potentially some low level json.text code or scan resources for tags in a separate sub process
        content = content
            .Replace("'\"", "{\"")
            .Replace("\"'", "\"}");

        var intermediateData = JsonSerializer.Deserialize<IntermediateUsageQueryResult>(content, jsonSerializerOptions) ?? throw new InvalidOperationException("failed to deserialize cost management api");

        var costIndex = intermediateData.Properties!.Columns!.FindIndex(x => x.Name == costColumnName);
        var resourceIdIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == resourceIdColumnName);
        var currencyIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == currencyColumnName);

        List<ResourceCost> costs = [];

        foreach (var row in intermediateData.Properties!.Rows!.Where((r) => !string.IsNullOrWhiteSpace((string)r[resourceIdIndex])))
        {
            List<KeyValuePair<string, string>> tags = [];
            
            costs.Add(new ResourceCost
            {
                Cost = (double)row[costIndex],
                ResourceId = (string)row[resourceIdIndex],
                Currency = (string)row[currencyIndex]
            });
        }

        return (intermediateData.Properties?.NextLink,  costs);
    }

    public class IntermediateUsageQueryResult
    {
        public PropertiesContext? Properties { get; set; }
    }


    public class Row : List<object>
    {
    }

    public class PropertiesContext
    {     
        public List<Column>? Columns { get; set; }
        public List<Row>? Rows { get; set; }
        public string NextLink { get; set; } = string.Empty;

    }

    public class Column
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}