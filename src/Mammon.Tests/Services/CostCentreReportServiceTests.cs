namespace Mammon.Tests.Services;

[TestClass]
[TestCategory("UnitTest")]
public class CostCentreReportServiceTests
{
	[TestMethod]
	public void ViewModelBuildTest()
	{
		//arrange
		Dictionary<string, CostCentreActorState> costCentreStates = new()
		{
			{
				"CostCentreA",
				new CostCentreActorState
				{
					ResourceCosts = new Dictionary<string, ResourceCost>
					{
						{ "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgA-envA/providers/microsoft.storage/storageaccounts/sampleSA", new ResourceCost(12, "EUR")},
						{ "/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/rgA-envB/providers/microsoft.storage/storageaccounts/sampleSA2", new ResourceCost(13, "EUR")},
						{ "/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/rgC-tokenA-envB/providers/microsoft.storage/storageaccounts/sampleSA2", new ResourceCost(13, "EUR")}
					},
					TotalCost = new ResourceCost(5000, "EUR")
				}
			},
			{
				"CostCentreB",
				new CostCentreActorState
				{
					ResourceCosts = new Dictionary<string, ResourceCost>
					{
						{ "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgC/providers/microsoft.storage/storageaccounts/sampleSA", new ResourceCost(14, "EUR")},
						{ "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgD/providers/microsoft.storage/storageaccounts/sampleSA2", new ResourceCost(15, "EUR")}
					},
					TotalCost = new ResourceCost(5000, "EUR")
				}
			}
		};

		var testRuleEngine = GetCostCentreRuleEngineInstance();
		var sut = new CostCentreReportService(Mock.Of<IConfiguration>(), testRuleEngine, new(testRuleEngine), Mock.Of<ServiceBusClient>(), Mock.Of<IServiceProvider>(), TimeProvider.System, Mock.Of<BlobServiceClient>());

		//act
		var modelBuilt= sut.BuildViewModel(ReportRequest, costCentreStates);

		//assert
		modelBuilt.Should().NotBeNull();
		modelBuilt.Root.Should().NotBeNull();
		modelBuilt.Root.SubNodes.Should().HaveCount(2);
		var costCentreANode = modelBuilt.Root.SubNodes["CostCentreA"];

		costCentreANode.SubNodes.Should().HaveCount(2); //RG
		costCentreANode.SubNodes.Should().ContainKey("rgA");
		costCentreANode.SubNodes.Should().ContainKey("classA"); //class grouping

		costCentreANode.SubNodes["rgA"].Leaves.Should().HaveCount(2); //environments
		costCentreANode.SubNodes["rgA"].Leaves.Should().Contain(x => x.Value.Name == "envA" && x.Value.Cost.Cost == 12); //environment
		costCentreANode.SubNodes["rgA"].Leaves.Should().Contain(x => x.Value.Name == "envB" && x.Value.Cost.Cost == 13); //environment
	}


	[DataTestMethod]
	[DataRow("2024-04-15 22:15:26", 3, "2024-03-03 00:00:00", "2024-04-02 23:59:59")] //ordinary date - cycle of 1st
	[DataRow("2024-04-15 22:15:26", 7, "2024-03-07 00:00:00", "2024-04-06 23:59:59")] //ordinary date - cycle of 7th
	[DataRow("2024-03-01 01:00:00", 1, "2024-02-01 00:00:00", "2024-02-29 23:59:59")] //leap year -cycle of 1st
	[DataRow("2023-03-01 01:00:00", 1, "2023-02-01 00:00:00", "2023-02-28 23:59:59")] //non leap year -cycle of 1st
	[DataRow("2024-01-01 01:00:00", 1, "2023-12-01 00:00:00", "2023-12-31 23:59:59")] //new year's -cycle of 1st
	[DataRow("2024-03-01 01:00:00", 7, "2024-02-07 00:00:00", "2024-03-06 23:59:59")] //leap year -cycle of 7th
	[DataRow("2023-03-01 01:00:00", 7, "2023-02-07 00:00:00", "2023-03-06 23:59:59")] //non leap year -cycle of 7th
	[DataRow("2024-01-01 01:00:00", 7, "2023-12-07 00:00:00", "2024-01-06 23:59:59")] //new year's -cycle of 7th
	public void GenerateDefaultReportRequestTest(string dtNow, int billingPeriodStart,  string expectedFromDT, string expectedToDT)
	{
		const string expectedDTFormat = "yyyy-MM-dd HH:mm:ss";

		//arrange
		var testTimeProvider = new FakeTimeProvider();
		
		testTimeProvider.SetUtcNow(new(DateTime.ParseExact(dtNow, expectedDTFormat, CultureInfo.InvariantCulture)));

		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.ReportBillingPeriodStartDayInMonthConfigKey, billingPeriodStart.ToString(CultureInfo.InvariantCulture))
		};

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings!)
			.Build();

		var ruleEngine = GetCostCentreRuleEngineInstance();
		var sut = new CostCentreReportService(configuration, ruleEngine, new(ruleEngine), Mock.Of<ServiceBusClient>(), Mock.Of<IServiceProvider>(), testTimeProvider, Mock.Of<BlobServiceClient>());

		//act
		var result = sut.GenerateDefaultReportRequest();

		//assert
		result.CostFrom.Should().Be(DateTime.ParseExact(expectedFromDT, expectedDTFormat, CultureInfo.InvariantCulture));
		result.CostTo.Should().Be(DateTime.ParseExact(expectedToDT, expectedDTFormat, CultureInfo.InvariantCulture));
	}	

	private static CostCentreRuleEngine GetCostCentreRuleEngineInstance()
	{
		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.CostCentreRuleEngineFilePathConfigKey, "./Services/testCostCentreReport.json"),
			new(Consts.CostCentreRuleEngineDevOpsConfigKey, "./Services/testCostCentreDevOpsRules.json")
		};

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings!)
			.Build();

		return new CostCentreRuleEngine(configuration);
	}

	private static CostReportRequest ReportRequest => new ()
	{
		ReportId = "testReportId",
		CostFrom = DateTime.Now,
		CostTo = DateTime.Now
	};
}
