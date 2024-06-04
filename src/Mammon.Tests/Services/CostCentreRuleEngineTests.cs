namespace Mammon.Tests.Services;

[TestClass]
[TestCategory("UnitTest")]
public class CostCentreRuleEngineTests
{
    [TestMethod]
    public void ShouldMatchSingleSpecificMultiTagCostCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgName/providers/microsoft.storage/storageaccounts/sampleSA",
            new Dictionary<string, string> { { "tagAName", "tagAValue" }, { "tagBName", "tagBValue" } });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(1);
        result.CostCentres.Should().Contain(new[] { "FullMatchMultiTag" });
    }

    [TestMethod]
    public void ShouldMatchSingleSpecificSingleTagCostCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgName/providers/microsoft.storage/storageaccounts/sampleSA",
            new Dictionary<string, string> { { "tagAName", "tagAValue" } });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(1);
        result.CostCentres.Should().Contain(new[] { "FullMatchSingleTag" });
    }

    [TestMethod]
    public void ShouldMatchSingleSplittableCostCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/71b2a3b5-f857-4926-b85c-1bd1f659ffb9/resourcegroups/otherRgName/providers/microsoft.containerservice/managedclusters/sampleSA",
            new Dictionary<string, string> { { "tagAName", "tagAValue" }, { "tagBName", "tagBValue" } });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(2);
        result.CostCentres.Should().Contain(new[] { "ResourceTypeMatch1", "ResourceTypeMatch2" });
        result.IsSplittable.Should().BeTrue();
    }

    [TestMethod]
    public void ShouldMatchResourceGroupLevelCostCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgName/providers/microsoft.storage/storageaccounts/otherSA",
            new Dictionary<string, string> { { "tagAName", "tagAValue" }, { "tagBName", "tagBValue" } });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(1);
        result.CostCentres.Should().Contain(new[] { "RGLevelMatch" });
    }

    [TestMethod]
    public void ShouldMatchSubscriptionLevelCostCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/otherRG/providers/microsoft.storage/storageaccounts/otherSA",
            new Dictionary<string, string> { { "tagAName", "tagAValue" }, { "tagBName", "tagBValue" } });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(1);
        result.CostCentres.Should().Contain(new[] { "SubLevelMatch" });
    }   

    [TestMethod]
    public void ShouldMatchTagsCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/blah/providers/microsoft.storage/storageaccounts/sampleSA",
            new Dictionary<string, string> { { "tagAName", "tagAValue" } });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(1);
        result.CostCentres.Should().Contain(new[] { "TagsMatch" });
    }

	[DataTestMethod]
	[DataRow("/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/blah/providers/microsoft.storage/storageaccounts/regexpResource-prod", "RegExpResourceNameMatch")]
	[DataRow("/subscriptions/030dc63d-963c-47bf-996e-cc0c32fc46ae/resourcegroups/regexpRG-prod/providers/microsoft.storage/storageaccounts/blah", "RegExpResourceGroupNameMatch")]
	[DataRow("/subscriptions/030dc63d-963c-47bf-996e-cc0c32fc46ae/resourcegroups/blah/providers/microsoft.regexptest/storageaccounts/blah", "RegExpResourceTypeNameMatch")]
	public void ShouldMatchRegExpRule(string resourceId, string expectedRuleName)
	{

		//act
		var result = InnerTest(
			resourceId,
			new Dictionary<string, string> { { "unusedTag", "tagAValue" } });

		//assert
		result.Should().NotBeNull();
		result.CostCentres.Length.Should().Be(1);
		result.CostCentres.Should().Contain(new[] { expectedRuleName });
	}

	[TestMethod]
    public void ShouldMatchDefaultCostCentreRule()
    {

        //act
        var result = InnerTest(
            "/subscriptions/030dc63d-963c-47bf-996e-cc0c32fc46ae/resourcegroups/blah/providers/microsoft.storage/storageaccounts/sampleSA",
            new Dictionary<string, string> { { "unusedTagName", "unusedTagValue"} });

        //assert
        result.Should().NotBeNull();
        result.CostCentres.Length.Should().Be(1);
        result.CostCentres.Should().Contain(new[] { "DefaultRuleMatch" });
        result.IsSplittable.Should().BeFalse();
        result.IsDefault.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("BlahTokenA", "21a25c3f-776a-408f-b319-f43e54634695", "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent",  "DevBox Pool")]
	[DataRow("BlahTokenA", "21a25c3f-776a-408f-b319-f43e54634695", "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/BlahTokenA/providers/microsoft.storage/storageaccounts/otherSA", "classA")]
	public void ClassifyResourceGroupTest(string pivotName, string subId, string resourceId, string? expected)
    {
		//act+assert
		GetInstance().ClassifyPivot(new CostReportPivotEntry() { PivotName = pivotName, SubscriptionId = subId, ResourceId = resourceId, Cost = new ResourceCost(1, "EUR")}).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("21a25c3f-776a-408f-b319-f43e54634695", "envA")]
	[DataRow("6a46ea4f-c676-437a-9298-41a1aacd7a51", "envB")]
	[DataRow("eeb60f0a-055b-489f-9966-561c357f5945", "unspecified")]
	[DataRow("N/A", "unspecified")]
	public void LookupEnvironmentTests(string resourceId, string expectedEnvironment)
    {
        //act+assert
        GetInstance().LookupEnvironment(resourceId).Should().Be(expectedEnvironment);
    }

	private CostCentreRule InnerTest(string resourceId, Dictionary<string, string> tags)
    {   
        //arrange       
        var ruleEngine = GetInstance();

        //act
        return ruleEngine.FindCostCentreRule(
            resourceId,
            tags);
    }

    private CostCentreRuleEngine GetInstance()
    {
		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.CostCentreRuleEngineFilePathConfigKey, "./Services/testCostCentreRules.json")
		};

		IConfiguration configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings!)
			.Build();

		return new CostCentreRuleEngine(configuration);
	}
}
