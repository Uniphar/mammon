using Azure.ResourceManager;
using MammonActors.Models.CostManagement;
using MammonActors.Utils;
using System.Text;
using System.Text.Json;

namespace MammonActors.Services
{
    public class CostManagementService
    {
        private readonly ArmClient armClient;
        private readonly HttpClient httpClient;
        private readonly ILogger<CostManagementService> logger;
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

        private const string costAPIVersion = "2023-11-01";

        private const string costColumnName = "Cost";
        private const string resourceIdColumnName = "ResourceId";
        private const string tagsColumnName = "Tags";
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

        public async Task<AzureCostResponse> QueryForSubAsync(string subName)
        {
            var subId = GetSubscriptionId(subName);
            if (string.IsNullOrWhiteSpace(subId))
                throw new InvalidOperationException($"Unable to find subscription {subName}");

            //TODO: derive time frame from actual report request (explicit or implied?)
            var request = "{\"type\":\"ActualCost\",\"dataSet\":{\"granularity\":\"None\",\"aggregation\":{\"totalCost\":{\"name\":\"Cost\",\"function\":\"Sum\"}},\"grouping\":[{\"type\":\"Dimension\",\"name\":\"ResourceId\"}],\"include\":[\"Tags\"]},\"timeframe\":\"Custom\",\"timePeriod\":{\"from\":\"2024-03-01T00:00:00+00:00\",\"to\":\"2024-03-31T23:59:59+00:00\"}}";

       
            var content = new StringContent(request, Encoding.UTF8, "application/json");

            //TODO: check no granularity support via https://learn.microsoft.com/en-us/dotnet/api/azure.resourcemanager.costmanagement.models.granularitytype.-ctor?view=azure-dotnet#azure-resourcemanager-costmanagement-models-granularitytype-ctor(system-string)

            var response = await httpClient.PostAsync(
                $"https://management.azure.com/{subId}/providers/Microsoft.CostManagement/query?api-version={costAPIVersion}",
            content);

            response.EnsureSuccessStatusCode();

            return ParseRawJson(await response.Content.ReadAsStringAsync());
           
            //TODO: implement nextLink support

            }

        private AzureCostResponse ParseRawJson(string content)
        {
            //ugly workaround to deal with invalid cost api response, alternatives are to write potentially some low level json.text code or scan resources for tags in a separate sub process
            content = content
                .Replace("'\"", "{\"")
                .Replace("\"'", "\"}");

            var intermediateData = JsonSerializer.Deserialize<IntermediateUsageQueryResult>(content, jsonSerializerOptions) ?? throw new InvalidOperationException("failed to deserialize cost management api");

            var costIndex = intermediateData.Properties!.Columns!.FindIndex(x => x.Name == costColumnName);
            var resourceIdIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == resourceIdColumnName);
            var tagsIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == tagsColumnName);
            var currencyIndex = intermediateData.Properties.Columns.FindIndex(x => x.Name == currencyColumnName);

            var cost = new AzureCostResponse();

            foreach (var row in intermediateData.Properties!.Rows!.Where((r) => !string.IsNullOrWhiteSpace((string)r[resourceIdIndex])))
            {
                cost.Add(new ResourceCost
                {
                    Cost = (double)row[costIndex],
                    ResourceId = (string)row[resourceIdIndex],
                    Currency = (string)row[currencyIndex]
                });
            }

            return cost;
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

        }

        public class Column
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }
    }
}