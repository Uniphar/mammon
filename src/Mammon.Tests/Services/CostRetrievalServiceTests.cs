namespace Mammon.Tests.Services;

[TestClass()]
[TestCategory("UnitTest")]
public class CostRetrievalServiceTests
{
    [TestMethod]
    public async Task ReturnsExpectedResponse()
    {
        //set up
        var mockHttp = new MockHttpMessageHandler();

        string sampleResponse = File.ReadAllText("./Services/costApiResponse.json");
        string sampleResponseNextLink = File.ReadAllText("./Services/costApiResponse-nextLink.json");

        mockHttp
            .When("https://management.azure.com/subscriptions/subId/providers/Microsoft.CostManagement/query?api-version=2024-01-01")
            .Respond("application/json", sampleResponse);

        mockHttp
           .When("https://nextLink")
           .Respond("application/json", sampleResponseNextLink);

        var client = mockHttp.ToHttpClient();

        var service = new TestCostRetrievalService(Mock.Of<ArmClient>(), mockHttp.ToHttpClient(), Mock.Of<ILogger<CostRetrievalService>>());

        //test
        var result = await service.QueryResourceCostForSubAsync(
            new ObtainCostsActivityRequest
			{
                ReportId = "some-report-id",
                SubscriptionName = "blah",               
                CostFrom = DateTime.UtcNow.AddDays(-1),
                CostTo = DateTime.UtcNow,
                PageIndex = 0,
                GroupingMode = GroupingMode.Resource
            });

        //assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);

        result.Should().Contain((r) => r.Cost.Cost == 1 && r.ResourceId == "resource1" && r.Cost.Currency=="EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        result.Should().Contain((r) => r.Cost.Cost == 2 && r.ResourceId == "resource2" && r.Cost.Currency == "EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        result.Should().Contain((r) => r.Cost.Cost == 3 && r.ResourceId == "resource3" && r.Cost.Currency == "EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        result.Should().Contain((r) => r.Cost.Cost == 4 && r.ResourceId == "resource4" && r.Cost.Currency == "EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [TestMethod]
    [DataRow("\"ms-resource-usage\":\"azure-cloud-shell\"", "ms-resource-usage", "azure-cloud-shell")]
    [DataRow("\"ms-resource-usage\":\"", "ms-resource-usage", "")]
    [DataRow("\"key1:key2\":\"s\"", "key1:key2", "s")]
    public void TestTagParsing(string rawInput, string expectedKey, string expectedValue)
    {
        //act
        var response = CostRetrievalService.ParseOutTag(rawInput);

        //assert
        response.Should().NotBeNull();
        response!.Value.Value.Should().Be(expectedValue);
        response!.Value.Key.Should().Be(expectedKey);

    }

    class TestCostRetrievalService(ArmClient armClient, HttpClient httpClient, ILogger<CostRetrievalService> logger) : CostRetrievalService(armClient, httpClient, Mock.Of<IConfiguration>(), logger)
    {
        //we need this as the GetSubscriptions() of the ArmClient is not mockable
        public override string? GetSubscriptionFullResourceId(string subscriptionName)
        {
            return "/subscriptions/subId";
        }
    }
}
