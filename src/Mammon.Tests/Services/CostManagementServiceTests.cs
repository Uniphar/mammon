namespace Mammon.UnitTests.Services;

[TestClass()]
[TestCategory("UnitTest")]
public class CostManagementServiceTests
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

        var service = new TestCostManagementService(Mock.Of<ArmClient>(), mockHttp.ToHttpClient(), Mock.Of<ILogger<CostManagementService>>(), Mock.Of<IConfiguration>());

        //test
        var result = await service.QueryForSubAsync(new CostReportSubscriptionRequest { SubscriptionName="blah", CostFrom = DateTime.UtcNow.AddDays(-1), CostTo = DateTime.UtcNow});

        //assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);

        result.Should().Contain((r) => r.Cost == 1d && r.ResourceId == "resource1" && r.Currency=="EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        result.Should().Contain((r) => r.Cost == 2d && r.ResourceId == "resource2" && r.Currency == "EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        result.Should().Contain((r) => r.Cost == 3d && r.ResourceId == "resource3" && r.Currency == "EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        result.Should().Contain((r) => r.Cost == 4d && r.ResourceId == "resource4" && r.Currency == "EUR"
            && r.Tags.Contains(new KeyValuePair<string, string>("tag1", "value1"))
            && r.Tags.Contains(new KeyValuePair<string, string>("tag2", "")));

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [DataTestMethod]
    [DataRow("\"ms-resource-usage\":\"azure-cloud-shell\"", "ms-resource-usage", "azure-cloud-shell")]
    [DataRow("\"ms-resource-usage\":\"", "ms-resource-usage", "")]
    [DataRow("\"key1:key2\":\"s\"", "key1:key2", "s")]
    public void TestTagParsing(string rawInput, string expectedKey, string expectedValue)
    {
        //act
        var response = CostManagementService.ParseOutTag(rawInput);

        //assert
        response.Should().NotBeNull();
        response!.Value.Value.Should().Be(expectedValue);
        response!.Value.Key.Should().Be(expectedKey);

    }

    class TestCostManagementService(ArmClient armClient, HttpClient httpClient, ILogger<CostManagementService> logger, IConfiguration configuration) : CostManagementService(armClient, httpClient, logger, configuration)
    {
        //we need this as the GetSubscriptions() of the ArmClient is not mockable
        public override string? GetSubscriptionId(string subscriptionName)
        {
            return "/subscriptions/subId";
        }
    }
}
