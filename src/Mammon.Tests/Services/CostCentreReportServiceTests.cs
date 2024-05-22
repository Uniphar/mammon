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
					ResourceCosts = new Dictionary<string, double>
					{
						{ "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgA-envA/providers/microsoft.storage/storageaccounts/sampleSA", 12 },
						{ "/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/rgA-envB/providers/microsoft.storage/storageaccounts/sampleSA2", 13 },
						{ "/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/rgC-tokenA-envB/providers/microsoft.storage/storageaccounts/sampleSA2", 13 }
					},
					TotalCost = 5000
				}
			},
			{
				"CostCentreB",
				new CostCentreActorState
				{
					ResourceCosts = new Dictionary<string, double>
					{
						{ "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgC/providers/microsoft.storage/storageaccounts/sampleSA", 14 },
						{ "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgD/providers/microsoft.storage/storageaccounts/sampleSA2", 15 }
					},
					TotalCost = 5000
				}
			}
		};

		var sut = new CostCentreReportService(Mock.Of<IConfiguration>(), GetCostCentreRuleEngineInstance(), Mock.Of<ServiceBusClient>(), Mock.Of<IServiceProvider>(), TimeProvider.System);

		//act
		var modelBuilt= sut.BuildViewModel(costCentreStates);

		//assert
		modelBuilt.Should().NotBeNull();
		modelBuilt.Root.Should().NotBeNull();
		modelBuilt.Root.SubNodes.Should().HaveCount(2);
		var costCentreANode = modelBuilt.Root.SubNodes["CostCentreA"];

		costCentreANode.SubNodes.Should().HaveCount(2); //RG
		costCentreANode.SubNodes.Should().ContainKey("rgA");
		costCentreANode.SubNodes.Should().ContainKey("classA"); //class grouping

		costCentreANode.SubNodes["rgA"].Leaves.Should().HaveCount(2); //environments
		costCentreANode.SubNodes["rgA"].Leaves.Should().Contain(new KeyValuePair<string, double>("envA", 12)); //environment
		costCentreANode.SubNodes["rgA"].Leaves.Should().Contain(new KeyValuePair<string, double>("envB", 13)); //environment
	}


	[DataTestMethod]
	[DataRow("2024-04-15 22:15:26", "2024-03-01 00:00:00", "2024-03-31 23:59:59")]
	[DataRow("2024-03-01 01:00:00", "2024-02-01 00:00:00", "2024-02-29 23:59:59")] //leap year
	[DataRow("2023-03-01 01:00:00", "2023-02-01 00:00:00", "2023-02-28 23:59:59")] //non leap year
	[DataRow("2024-01-01 01:00:00", "2023-12-01 00:00:00", "2023-12-31 23:59:59")] //new year's
	public void GenerateDefaultReportRequestTest(string dtNow, string expectedFromDT, string expectedToDT)
	{
		const string expectedDTFormat = "yyyy-MM-dd HH:mm:ss";

		//arrange
		var testTimeProvider = new FakeTimeProvider();
		
		testTimeProvider.SetUtcNow(new(DateTime.ParseExact(dtNow, expectedDTFormat, CultureInfo.InvariantCulture)));
		
		var sut = new CostCentreReportService(Mock.Of<IConfiguration>(), GetCostCentreRuleEngineInstance(), Mock.Of<ServiceBusClient>(), Mock.Of<IServiceProvider>(), testTimeProvider);

		//act
		var result = sut.GenerateDefaultReportRequest();

		//assert
		result.CostFrom.Should().Be(DateTime.ParseExact(expectedFromDT, expectedDTFormat, CultureInfo.InvariantCulture));
		result.CostTo.Should().Be(DateTime.ParseExact(expectedToDT, expectedDTFormat, CultureInfo.InvariantCulture));
	}

	private static CostCentreRuleEngine GetCostCentreRuleEngineInstance()
	{
		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.CostCentreRuleEngineFilePathConfigKey, "./Services/testCostCentreReport.json")
		};

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings!)
			.Build();

		return new CostCentreRuleEngine(configuration);
	}
}
