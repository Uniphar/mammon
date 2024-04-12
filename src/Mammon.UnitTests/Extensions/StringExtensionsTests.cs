using FluentAssertions;
using Mammon.Extensions;

namespace Mammon.UnitTests.Extensions
{
    [TestClass, TestCategory("UnitTest")]
    public class StringExtensionsTests
    {
        [DataTestMethod]
        [DataRow("/subscriptions/961b7071-1490-44c4-97b7-b4620c73aee5/resourceGroups/compute-ne-prod/providers/Microsoft.ContainerService/managedClusters/compute-aks-ne-prod-k8s/providers/blah", "/subscriptions/961b7071-1490-44c4-97b7-b4620c73aee5/resourceGroups/compute-ne-prod/providers/Microsoft.ContainerService/managedClusters/compute-aks-ne-prod-k8s")]
        public void ToResourceActorIdTests(string input, string expected)
        {
            input.ToResourceActorId().Should().Be(expected);
        }
    }
}
