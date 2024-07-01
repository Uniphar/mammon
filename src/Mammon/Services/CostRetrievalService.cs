namespace Mammon.Services;

public class CostRetrievalService
{
    private readonly ArmClient armClient;
    private readonly HttpClient httpClient;
    private readonly ILogger<CostRetrievalService> logger;
    private readonly IConfiguration configuration;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    private const string costAPIVersion = "2024-01-01";

    private const string costColumnName = "Cost";
    private const string resourceIdColumnName = "ResourceId";
	private const string subscriptionIdColumnName = "SubscriptionId";
	private const string currencyColumnName = "Currency";
    private const string tagsColumnName = "Tags";

    public CostRetrievalService(ArmClient armClient, HttpClient httpClient, ILogger<CostRetrievalService> logger, IConfiguration configuration)
    {
        this.armClient = armClient;
        this.httpClient = httpClient;
        this.logger = logger;
        this.configuration = configuration;
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
    }

    public virtual string? GetSubscriptionFullResourceId(string subscriptionName)
    {
        var subByName = armClient.GetSubscriptions().FirstOrDefault((s) => s.Data.DisplayName == subscriptionName);

        return subByName?.Id ?? string.Empty;
    }

    public async Task<AzureCostResponse> QueryForSubAsync(CostReportSubscriptionRequest request)
    {
        new CostReportSubscriptionRequestValidator().ValidateAndThrow(request);

        var subId = GetSubscriptionFullResourceId(request.SubscriptionName);
        if (string.IsNullOrWhiteSpace(subId))
            throw new InvalidOperationException($"Unable to find subscription {request.SubscriptionName}");

        var groupingProperty = request.GroupingMode==GroupingMode.Resource ? "ResourceId" : "SubscriptionId";

        var costApirequest = $"{{\"type\":\"ActualCost\",\"dataSet\":{{\"granularity\":\"None\",\"aggregation\":{{\"totalCost\":{{\"name\":\"Cost\",\"function\":\"Sum\"}}}},\"grouping\":[{{\"type\":\"Dimension\",\"name\":\"{groupingProperty}\"}}],\"include\":[\"Tags\"]}},\"timeframe\":\"Custom\",\"timePeriod\":{{\"from\":\"{request.ReportRequest.CostFrom:yyyy-MM-ddTHH:mm:ss+00:00}\",\"to\":\"{request.ReportRequest.CostTo:yyyy-MM-ddTHH:mm:ss+00:00}\"}}}}";

        //TODO: check no granularity support via https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.costmanagement.models.granularitytype.-ctor?view=azure-dotnet#azure-resourcemanager-costmanagement-models-granularitytype-ctor(system-string)
        HttpResponseMessage response;
        
        string? url = $"https://management.azure.com{subId}/providers/Microsoft.CostManagement/query?api-version={costAPIVersion}";
        bool nextPageAvailable;

        AzureCostResponse responseData = [];

        do
        {           
            string? nextLink;
            List<ResourceCostResponse> costs;

#if (DEBUG)
			string? mockApiResponsePath;

			if (!string.IsNullOrWhiteSpace(mockApiResponsePath = configuration[Consts.MockCostAPIResponseFilePathConfigKey]) 
                && File.Exists(mockApiResponsePath))
            {
                var mockResponse = File.ReadAllText(mockApiResponsePath);
                (nextLink, costs) = ParseRawJson(mockResponse, subId, GroupingMode.Resource); //mock only supports resource grouping at the moment
            }
            else
            {
#endif
                var requestContent = new StringContent(costApirequest, Encoding.UTF8, "application/json");

                response = await httpClient.PostAsync(url, requestContent);

                response.EnsureSuccessStatusCode();

                (nextLink, costs) = ParseRawJson(await response.Content.ReadAsStringAsync(), subId, request.GroupingMode);
#if (DEBUG)
            }
#endif

            responseData.AddRange(costs);

            nextPageAvailable = !string.IsNullOrWhiteSpace(nextLink);
            url = nextLink;

        }
        while (nextPageAvailable);

        return responseData;
    }

    private (string? nextLink, List<ResourceCostResponse> costs) ParseRawJson(string content, string subId, GroupingMode groupingMode)
    {
        //ugly workaround to deal with invalid cost api response, alternatives are to write potentially some low level json.text code or scan resources for tags in a separate sub process
        content = content
            .Replace("'\"", "{\"")
            .Replace("\"'", "\"}");

        var intermediateData = JsonSerializer.Deserialize<IntermediateUsageQueryResult>(content, jsonSerializerOptions) ?? throw new InvalidOperationException("failed to deserialize cost management api");

        var costIndex = intermediateData.Properties!.Columns!.FindIndex(x => x.Name == costColumnName);
        var resourceIdIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name ==  (groupingMode==GroupingMode.Resource ? resourceIdColumnName : subscriptionIdColumnName));
        var currencyIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == currencyColumnName);
        var tagsId = intermediateData.Properties.Columns.FindIndex(x => x.Name == tagsColumnName);

        List<ResourceCostResponse> costs = [];

        foreach (var row in intermediateData.Properties!.Rows!)
        {
            Dictionary<string, string> tags = [];

            foreach (var item in ((JsonElement) row[tagsId]).EnumerateArray())
            {
                string value = item.ToString();
                var tag = ParseOutTag(value);
                if (tag != null)
                {
                    tags.TryAdd(tag.Value.Key, tag.Value.Value);
                }

            }

            var rID = (string)row[resourceIdIndex];

            ///handle edge case of missing resource id (often external marketplace)
            ///assign virtual one
            if (string.IsNullOrWhiteSpace(rID))
                rID = $"{subId}/resourceGroups/unknown/providers/unknown/unknown/{Guid.NewGuid()}";

			costs.Add(new ResourceCostResponse
            {
                Cost = new ResourceCost((decimal)row[costIndex], (string)row[currencyIndex]),
				ResourceId = rID,
                Tags = tags
            });
        }

        return (intermediateData.Properties?.NextLink,  costs);
    }

    public static KeyValuePair<string, string>? ParseOutTag(string? value)
    {
        //TODO: extract examples with special characters (esp. : and " in key or value) and reevaluate unit tests
        const string splitter = "\":\"";

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!value.Contains(splitter))
            return null;

        int index = value.IndexOf(splitter);
        int keyStartIndex = 1;
        int keyLength = index - 1;
        int valueStartIndex = index + splitter.Length;
        int valueLength = value.Length-valueStartIndex-1;

        if (valueLength < 0)
            valueLength = 0;

        return new KeyValuePair<string, string>(value.Substring(keyStartIndex, keyLength), value.Substring(valueStartIndex,valueLength));
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