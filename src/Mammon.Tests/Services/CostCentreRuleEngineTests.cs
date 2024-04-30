namespace Mammon.Tests.Services;

[TestClass]
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

    private CostCentreRule InnerTest(string resourceId, Dictionary<string, string> tags)
    {   
        //arrange
        var inMemorySettings = new List<KeyValuePair<string, string>> {
            new(Consts.CostCentreRuleEngineFilePathConfigKey, "./Services/testCostCentreRules.json")
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var ruleEngine = new CostCentreRuleEngine(configuration);

        //act
        return ruleEngine.FindCostCentreRule(
            resourceId,
            tags);
    }
}
