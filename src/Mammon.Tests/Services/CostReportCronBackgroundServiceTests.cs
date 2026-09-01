namespace Mammon.Tests.Services;

[TestClass]
[TestCategory("UnitTest")]
public class CostReportCronBackgroundServiceTests
{
	private const string ExpectedDTFormat = "yyyy-MM-dd HH:mm:ss";

	[TestMethod]
	[DataRow("2024-04-01 01:00:00", 1, true)] //due day, on/after the scheduled hour
	[DataRow("2024-04-01 00:59:00", 1, false)] //due day, but before the scheduled hour
	[DataRow("2024-04-02 01:00:00", 1, false)] //not the due day
	public async Task PublishesReportRequestOnlyWhenScheduleIsDue(string dtNow, int billingPeriodStart, bool expectPublish)
	{
		//arrange
		var testTimeProvider = new FakeTimeProvider();
		testTimeProvider.SetUtcNow(new(DateTime.ParseExact(dtNow, ExpectedDTFormat, CultureInfo.InvariantCulture)));

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection([new(Consts.ReportBillingPeriodStartDayInMonthConfigKey, billingPeriodStart.ToString(CultureInfo.InvariantCulture))])
			.Build();

		var ruleEngine = new CostCentreRuleEngine(new ConfigurationBuilder()
			.AddInMemoryCollection([
				new(Consts.CostCentreRuleEngineFilePathConfigKey, "./Services/testCostCentreReport.json"),
				new(Consts.CostCentreRuleEngineDevOpsConfigKey, "./Services/testCostCentreDevOpsRules.json")
			])
			.Build());

		var reportService = new CostCentreReportService(configuration, ruleEngine, new(ruleEngine, Mock.Of<IActorProxyFactory>()), Mock.Of<ServiceBusClient>(), Mock.Of<IServiceProvider>(), testTimeProvider, Mock.Of<BlobServiceClient>());

		var daprClientMock = new Mock<DaprClient>();

		var sut = new CostReportCronBackgroundService(daprClientMock.Object, reportService, configuration, testTimeProvider, Mock.Of<ILogger<CostReportCronBackgroundService>>());

		//act
		await sut.StartAsync(CancellationToken.None);
		await sut.StopAsync(CancellationToken.None);

		//assert
		daprClientMock.Verify(
			d => d.PublishEventAsync(Consts.MammonPubSubCRDName, Consts.MammonServiceBusTopicName, It.IsAny<CostReportRequest>(), It.IsAny<CancellationToken>()),
			expectPublish ? Times.Once : Times.Never);
	}
}
