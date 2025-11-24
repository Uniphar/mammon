namespace Mammon.Services;

public class CostRetrievalService
{
    private readonly ArmClient armClient;
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly ILogger<CostRetrievalService> logger;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    private const string costAPIVersion = "2024-01-01";

    private const string costColumnName = "Cost";
    private const string resourceIdColumnName = "ResourceId";
    private const string subscriptionIdColumnName = "SubscriptionId";
    private const string currencyColumnName = "Currency";
    private const string tagsColumnName = "Tags";

    private const int PageSize = 1000; //TODO: make configurable?

    public CostRetrievalService(
        ArmClient armClient,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CostRetrievalService> logger)
    {
        this.armClient = armClient;
        this.httpClient = httpClient;
        this.configuration = configuration;
        this.logger = logger;
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
    }

    public virtual string? GetSubscriptionFullResourceId(string subscriptionName)
    {
        var subByName = armClient.GetSubscriptions().FirstOrDefault((s) => s.Data.DisplayName == subscriptionName);

        return subByName?.Id ?? string.Empty;
    }

    public async Task<List<DevOpsCostResponse>> QueryDevOpsCostForSubAsync(ObtainDevOpsCostsActivityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DevOpsOrganization)) return [];

        var costApiRequest = $$"""
                               {
                                   "type": "ActualCost",
                                   "dataSet": {
                                   "granularity": "None",
                                   "aggregation": {
                                       "totalCost": {
                                       "name": "Cost",
                                       "function": "Sum"
                                       }
                                   },
                                   "grouping": [
                                       { "type": "Dimension", "name": "Product" },
                                       { "type": "Dimension", "name": "MeterSubcategory" }
                                   ],
                                   "include": [ "Tags" ],
                                   "filter": {
                                       "tags": {
                                       "name": "_organizationname_",
                                       "operator": "In",
                                       "values": [ "{{request.DevOpsOrganization}}" ]
                                       }
                                   }
                                   },
                                   "timeframe": "Custom",
                                   "timePeriod": {
                                   "from": "{{request.CostFrom:yyyy-MM-ddTHH:mm:ss+00:00}}",
                                   "to": "{{request.CostTo:yyyy-MM-ddTHH:mm:ss+00:00}}"
                                   }
                               }
                       """;

        bool nextPageAvailable;
        List<DevOpsCostResponse> responseData = [];

        string? nextLink = null;
        string? url = null;
        string? subId = null;

        do
        {
            List<DevOpsCostResponse> costs;

#if (DEBUG || INTTEST)
            string? mockApiResponsePath;
            if (!string.IsNullOrWhiteSpace(mockApiResponsePath = configuration[Consts.MockDevOpsCostAPIResponseFilePathConfigKey])
                && File.Exists(mockApiResponsePath))
            {
                var mockResponse = await File.ReadAllTextAsync(mockApiResponsePath);
                (nextLink, costs) = ParseDevOpsRawJson(mockResponse);
            }
            else
            {
            
#endif
                if (string.IsNullOrWhiteSpace(subId))
                {
                    subId = GetSubscriptionFullResourceId(request.SubscriptionName);
                    if (string.IsNullOrWhiteSpace(subId))
                        throw new InvalidOperationException($"Unable to find subscription {request.SubscriptionName}");
                    url = $"https://management.azure.com{subId}/providers/Microsoft.CostManagement/query?api-version={costAPIVersion}";
                }

                var requestContent = new StringContent(costApiRequest, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(url, requestContent);

                response.EnsureSuccessStatusCode();

                (nextLink, costs) = ParseDevOpsRawJson(await response.Content.ReadAsStringAsync());
#if (DEBUG || INTTEST)
            }
#endif

            responseData.AddRange(costs);
            nextPageAvailable = !string.IsNullOrWhiteSpace(nextLink);
            url = nextLink;
        } while(nextPageAvailable);

        return responseData;
    }

    public async Task<AzureCostResponse> QueryResourceCostForSubAsync(ObtainCostsActivityRequest request)
    {
        //new CostReportSubscriptionRequestValidator().ValidateAndThrow(request);

        var groupingProperty = request.GroupingMode == GroupingMode.Resource ? "ResourceId" : "SubscriptionId";

        var costApirequest = $@"
        {{
          ""type"": ""ActualCost"",
          ""dataSet"": {{
            ""granularity"": ""None"",
            ""aggregation"": {{
              ""totalCost"": {{
                ""name"": ""Cost"",
                ""function"": ""Sum""
              }}
            }},
            ""grouping"": [
              {{
                ""type"": ""Dimension"",
                ""name"": ""{groupingProperty}""
              }}
            ],
            ""include"": [ ""Tags"" ]
          }},
          ""timeframe"": ""Custom"",
          ""timePeriod"": {{
            ""from"": ""{request.CostFrom:yyyy-MM-ddTHH:mm:ss+00:00}"",
            ""to"": ""{request.CostTo:yyyy-MM-ddTHH:mm:ss+00:00}""
          }}
        }}";


        //TODO: check no granularity support via https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.costmanagement.models.granularitytype.-ctor?view=azure-dotnet#azure-resourcemanager-costmanagement-models-granularitytype-ctor(system-string)
        bool nextPageAvailable;
        List<ResourceCostResponse> responseData = [];

        string? nextLink;
        string? url = null;
        string? subId = null;

        do
        {
            List<ResourceCostResponse> costs;

#if (INTTEST)
            string? mockApiResponsePath;

            if (!string.IsNullOrWhiteSpace(mockApiResponsePath = configuration[Consts.MockCostAPIResponseFilePathConfigKey])
                && File.Exists(mockApiResponsePath))
            {
                var mockResponse = await File.ReadAllTextAsync(mockApiResponsePath);

                using var doc = JsonDocument.Parse(mockResponse);
                var subscriptionJson = doc.RootElement.GetProperty(request.SubscriptionName).GetRawText();

                (nextLink, costs) = ParseRawJson(subscriptionJson, $"subscriptions/{Guid.Empty}", GroupingMode.Resource); //mock only supports resource grouping at the moment
            }
            else
            {
#endif
                if (string.IsNullOrWhiteSpace(subId))
                {
                    subId = GetSubscriptionFullResourceId(request.SubscriptionName);
                    if (string.IsNullOrWhiteSpace(subId))
                        throw new InvalidOperationException($"Unable to find subscription {request.SubscriptionName}");

                    url = $"https://management.azure.com{subId}/providers/Microsoft.CostManagement/query?api-version={costAPIVersion}";
                }

                var requestContent = new StringContent(costApirequest, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, requestContent);

                response.EnsureSuccessStatusCode();

                (nextLink, costs) = ParseRawJson(await response.Content.ReadAsStringAsync(), subId, request.GroupingMode);
#if (INTTEST)
            }
#endif

            responseData.AddRange(costs);

            nextPageAvailable = !string.IsNullOrWhiteSpace(nextLink);
            url = nextLink;
        } while (nextPageAvailable);

        //extract sub page
        int startIndex = request.PageIndex * PageSize;
        int endIndex = startIndex + PageSize;

        var records = responseData.GetRange(startIndex, responseData.Count < endIndex ? responseData.Count - startIndex : PageSize);

        return new AzureCostResponse(records, request.PageIndex, responseData.Count > (endIndex + 1));
    }

    private (string? nextLink, List<ResourceCostResponse> costs) ParseRawJson(string content, string subId, GroupingMode groupingMode)
    {
        //ugly workaround to deal with invalid cost api response, alternatives are to write potentially some low level json.text code or scan resources for tags in a separate sub process
        content = content
            .Replace("'\"", "{\"")
            .Replace("\"'", "\"}");

        var intermediateData = JsonSerializer.Deserialize<IntermediateUsageQueryResult>(content, jsonSerializerOptions) ??
                               throw new InvalidOperationException("failed to deserialize cost management api");

        var costIndex = intermediateData.Properties!.Columns!.FindIndex(x => x.Name == costColumnName);
        var resourceIdIndex =
            intermediateData.Properties.Columns.FindIndex(x => x.Name == (groupingMode == GroupingMode.Resource ? resourceIdColumnName : subscriptionIdColumnName));
        var currencyIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == currencyColumnName);
        var tagsId = intermediateData.Properties.Columns.FindIndex(x => x.Name == tagsColumnName);

        List<ResourceCostResponse> costs = [];

        foreach (var row in intermediateData.Properties!.Rows!)
        {
            Dictionary<string, string> tags = [];

            foreach (var item in ((JsonElement)row[tagsId]).EnumerateArray())
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

        return (intermediateData.Properties?.NextLink, costs);
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
        int valueLength = value.Length - valueStartIndex - 1;

        if (valueLength < 0)
            valueLength = 0;

        return new KeyValuePair<string, string>(value.Substring(keyStartIndex, keyLength), value.Substring(valueStartIndex, valueLength));
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

    private (string? nextLink, List<DevOpsCostResponse> costs) ParseDevOpsRawJson(string content)
    {
        // Same workaround you already use
        content = content
            .Replace("'\"", "{\"")
            .Replace("\"'", "\"}");

        var result = JsonSerializer.Deserialize<IntermediateUsageQueryResult>(content, jsonSerializerOptions)
                     ?? throw new InvalidOperationException("failed to deserialize DevOps cost management api");

        var columns = result.Properties!.Columns!;
        var rows = result.Properties.Rows!;

        int costIndex = columns.FindIndex(x => x.Name == "Cost");
        int productIndex = columns.FindIndex(x => x.Name == "Product");
        int meterSubcatIndex = columns.FindIndex(x => x.Name == "MeterSubcategory");
        int tagsIndex = columns.FindIndex(x => x.Name == "Tags");
        int currencyIndex = columns.FindIndex(x => x.Name == "Currency");

        List<DevOpsCostResponse> costs = new();

        foreach (var row in rows)
        {
            // Parse tags → identical logic to your existing parser
            Dictionary<string, string> tags = new();

            foreach (var tagElement in ((JsonElement)row[tagsIndex]).EnumerateArray())
            {
                var parsed = ParseOutTag(tagElement.ToString());
                if (parsed != null)
                    tags.TryAdd(parsed.Value.Key, parsed.Value.Value);
            }

            costs.Add(new DevOpsCostResponse
            {
                Cost = new ResourceCost((decimal)row[costIndex], (string)row[currencyIndex]),
                Product = (string)row[productIndex],
                MeterSubcategory = (string)row[meterSubcatIndex],
                Tags = tags
            });
        }

        return (result.Properties!.NextLink, costs);
    }

    public sealed class DevOpsCostResponse
    {
        public ResourceCost Cost { get; set; } = default!;
        public string Product { get; set; } = string.Empty;
        public string MeterSubcategory { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}