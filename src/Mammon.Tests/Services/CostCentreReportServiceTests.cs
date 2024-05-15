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

		var sut = new CostCentreReportService(Mock.Of<IServiceProvider>(), GetCostCentreRuleEngineInstance());
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
