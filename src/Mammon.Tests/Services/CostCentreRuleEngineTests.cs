using Mammon.Tests.Actors;

namespace Mammon.Tests.Services;

[TestClass]
[TestCategory("UnitTest")]
public class CostCentreRuleEngineTests : BaseUnitTests
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
        result.CostCentre.Should().Be("FullMatchMultiTag");
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
        result.CostCentre.Should().Be("FullMatchSingleTag");
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
        result.CostCentre.Should().Be("RGLevelMatch");
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
        result.CostCentre.Should().Be("SubLevelMatch" );
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
        result.CostCentre.Should().Be("TagsMatch");
    }

	[TestMethod]
	[DataRow("/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/blah/providers/microsoft.storage/storageaccounts/regexpResource-prod", "RegExpResourceNameMatch")]
	[DataRow("/subscriptions/030dc63d-963c-47bf-996e-cc0c32fc46ae/resourcegroups/regexpRG-prod/providers/microsoft.storage/storageaccounts/blah", "RegExpResourceGroupNameMatch")]
	[DataRow("/subscriptions/030dc63d-963c-47bf-996e-cc0c32fc46ae/resourcegroups/blah/providers/microsoft.regexptest/storageaccounts/blah", "RegExpResourceTypeNameMatch")]
	[DataRow("/subscriptions/6a46ea4f-c676-437a-9298-41a1aacd7a51/resourcegroups/blah/providers/microsoft.devcenter/projects/testDevBoxProject/pools/poolA", "RegExpFullIdMatch")]
	public void ShouldMatchRegExpRule(string resourceId, string expectedRuleName)
	{

		//act
		var result = InnerTest(
			resourceId,
			new Dictionary<string, string> { { "unusedTag", "tagAValue" } });

		//assert
		result.Should().NotBeNull();
		result.CostCentre.Should().Be(expectedRuleName);
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
        result.CostCentre.Should().Be("DefaultCostCentre");
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

    [TestMethod]
    [DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourceGroups/vdi-sample-dev/providers/Microsoft.Compute/virtualMachines/vm-sample-dev1", true)]
    public void IsModeEnabledTest(string resourceId, bool expected)
    {
        //act+assert
        GetInstance().IsModeEnabled("VDISplit", new ResourceIdentifier(resourceId)).Should().Be(expected);
    }

	[TestMethod]
	[DataRow("8ce21b7c-0173-4852-a94c-eb3a5cd43dc2", "CostCentreA")]
	[DataRow("265ba43f-5775-4cc2-b2e1-f80dff1c74b9", "CostCentreB")]
	[DataRow("e71935e8-1bb8-4033-9884-1312b5ca2b56", "DefaultCostCentre")]
	public void GetCostCentreForGroupIDTests(string groupID, string expectedCostCentre)
	{
		//act+assert
		GetInstance().GetCostCentreForGroupID(groupID).Should().Be(expectedCostCentre);
	}

	[TestMethod]
	[DataRow("NSA", "CostCentreA")]
	[DataRow("NSB", "CostCentreB")]
	[DataRow("NSC", "DefaultCostCentre")]
	public void GetCostCentreForAKSNamespaceTests(string ns, string expectedCostCentre)
	{
		//act+assert
		GetInstance().GetCostCentreForAKSNamespace(ns).Should().Be(expectedCostCentre);
	}

	[TestMethod]
	[DataRow("SAMPLEdb-dev-db", "CostCentreA")]
	[DataRow("sampledb-prod-db", "CostCentreA")]
	[DataRow("other-db", "DefaultCostCentre")]
	public void GetCostCentreForSQLDatabaseTests(string db, string expectedCostCentre)
	{
		//act+assert
		GetInstance().GetCostCentreForSQLDatabase(db).Should().Be(expectedCostCentre);
	}

    [TestMethod]
    public void StaticMySQLMappingTest()
    {
        //act
        var split = GetInstance().StaticMySQLMapping;

        //assert
        split.Should().NotBeNull();
        split.Should().HaveCount(2);
        split.Should().Contain(split => split.Key == "CostCentreA" && split.Value == 20);
		split.Should().Contain(split => split.Key == "CostCentreB" && split.Value == 80);
	}
	private static CostCentreRule InnerTest(string resourceId, Dictionary<string, string> tags)
    {   
        //arrange       
        var ruleEngine = GetInstance();

        //act
        return ruleEngine.FindCostCentreRule(
            resourceId,
            tags);
    }
}
