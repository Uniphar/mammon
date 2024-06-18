namespace Mammon.Tests.Extensions;

[TestClass]
[TestCategory("UnitTest")]
public class ResourceIdentifierTests
{
	[DataTestMethod]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent", "customPool-dotnet")]
	public void GetDevBoxProjectNameTests(string input, string expectedPoolName)
	{
		//arrange
		var rid = new ResourceIdentifier(input);

		//act+assert
		rid.GetDevBoxProjectName().Should().Be(expectedPoolName);
	}

	[DataTestMethod]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent", false)]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/core-env-dev/providers/microsoft.operationalinsights/workspaces/test-logs", true)]
	public void IsLogAnalyticsWorkspaceTests(string input, bool expected)
	{
		//arrange
		ResourceIdentifier rID = new(input);

		//act+assert
		rID.IsLogAnalyticsWorkspace().Should().Be(expected);
	}
}
