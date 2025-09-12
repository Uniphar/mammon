namespace Mammon.Tests.Extensions;

[TestClass]
[TestCategory("UnitTest")]
public class ResourceIdentifierTests
{
	[TestMethod]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent", "customPool-dotnet")]
	public void GetDevBoxProjectNameTests(string input, string expectedPoolName)
	{
		//arrange
		var rid = new ResourceIdentifier(input);

		//act+assert
		rid.GetDevBoxProjectName().Should().Be(expectedPoolName);
	}

	[TestMethod]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent", false)]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/core-env-dev/providers/microsoft.operationalinsights/workspaces/test-logs", true)]
	public void IsLogAnalyticsWorkspaceTests(string input, bool expected)
	{
		//arrange
		ResourceIdentifier rID = new(input);

		//act+assert
		rID.IsLogAnalyticsWorkspace().Should().Be(expected);
	}

	[TestMethod]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent", "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox")]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox", "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox")]
	public void GetResourceGroupIdentifierTest(string input, string expected)
	{
		//arrange
		ResourceIdentifier rID = new(input);

		//act+assert
		rID.GetResourceGroupIdentifier().Should().Be(new ResourceIdentifier(expected));
	}
}
