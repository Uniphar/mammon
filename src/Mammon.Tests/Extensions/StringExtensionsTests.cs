namespace Mammon.UnitTests.Extensions;

[TestClass, TestCategory("UnitTest")]
public class StringExtensionsTests
{
	[DataTestMethod]
	[DataRow("/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS/providers/blah", "/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS")]
	public void ToResourceActorIdTests(string input, string expected)
	{
		input.ToParentResourceId().Should().Be(expected);
	}

	[DataTestMethod]
	[DataRow("CoreSuffix", "Core", "Suffix")]
	[DataRow("CoreSuffix", "CoreSuffix", "blah")]
	[DataRow("CoreSuffix-NE", "Core", "Suffix-NE", "Suffix")]
	[DataRow("CoreSuffix", "Core", "Suffix-NE", "Suffix")]
	[DataRow("CoreSuffix", "CoreSuffix", null)]
	[DataRow(null, null, null)]
	public void RemoveSuffixesTests(string input, string expected, params string[] suffixes)
	{
		var ret = input.RemoveSuffixes(suffixes);

		ret.Should().Be(expected);
	}

	[DataTestMethod]
	[DataRow("a@b.c, d@e.f ", "a@b.c", "d@e.f")]
	[DataRow("a@b.c, ", "a@b.c")]
	[DataRow("")]
	[DataRow(null)]
	public void SplitEmailContactsTests(string input, params string[] expectedItems)
	{
		var ret = input.SplitEmailContacts();

		ret.Should().BeEquivalentTo(expectedItems);
	}
}
