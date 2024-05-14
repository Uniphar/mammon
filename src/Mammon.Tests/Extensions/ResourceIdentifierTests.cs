namespace Mammon.Tests.Extensions;

[TestClass]
[TestCategory("UnitTest")]
public class ResourceIdentifierTests
{
	[DataTestMethod]
	[DataRow("/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/testDevBox/providers/microsoft.devcenter/projects/customPool-dotnet/pools/custom-dotnet-vs2022ent", "customPool-dotnet")]
	public void GetDevBoxPoolNameTests(string input, string expectedPoolName)
	{
		//arrange
		var rid = new ResourceIdentifier(input);

		//act+assert
		rid.GetDevBoxPoolName().Should().Be(expectedPoolName);
	}
}
