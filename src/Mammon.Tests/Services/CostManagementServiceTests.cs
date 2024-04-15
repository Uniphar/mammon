﻿namespace Mammon.UnitTests.Services;

[TestClass()]
[TestCategory("UnitTest")]
public class CostManagementServiceTests
{
    [TestMethod]
    public async Task ReturnsExpectedResponse()
    {
        //set up
        var mockHttp = new MockHttpMessageHandler();

        string sampleResponse = File.ReadAllText("./Services/costApiResponse.txt");

        mockHttp
            .When("https://management.azure.com/subscriptions/subId/providers/Microsoft.CostManagement/query?api-version=2023-11-01")
            .Respond("application/json", sampleResponse);

        var client = mockHttp.ToHttpClient();

        var service = new TestCostManagementService(Mock.Of<ArmClient>(), mockHttp.ToHttpClient(), Mock.Of<ILogger<CostManagementService>>());

        //test
        var result = await service.QueryForSubAsync(new CostReportRequest { SubscriptionName="blah", CostFrom = DateTime.UtcNow.AddDays(-1), CostTo = DateTime.UtcNow});

        //assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain((r) => r.Cost == 73.96 && r.ResourceId == "resource1");
        result.Should().Contain((r) => r.Cost == 12.34 && r.ResourceId == "resource2");
    }        

    class TestCostManagementService(ArmClient armClient, HttpClient httpClient, ILogger<CostManagementService> logger) : CostManagementService(armClient, httpClient, logger)
    {
        //we need this as the GetSubscriptions() of the ArmClient is not mockable
        public override string? GetSubscriptionId(string subscriptionName)
        {
            return "/subscriptions/subId";
        }
    }
}
