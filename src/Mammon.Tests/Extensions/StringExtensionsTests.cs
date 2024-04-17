namespace Mammon.UnitTests.Extensions;

[TestClass, TestCategory("UnitTest")]
public class StringExtensionsTests
{
    [DataTestMethod]
    [DataRow("/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS/providers/blah", "/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS")]
    public void ToResourceActorIdTests(string input, string expected)
    {
        input.ToResourceActorId().Should().Be(expected);
    }
}
