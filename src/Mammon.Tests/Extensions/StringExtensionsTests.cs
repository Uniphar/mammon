namespace Mammon.Tests.Extensions;

[TestClass, TestCategory("UnitTest")]
public class StringExtensionsTests
{
	[DataTestMethod]
	[DataRow("/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS/providers/blah", "/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS")]
	[DataRow("/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS/extensions/blah", "/subscriptions/blah/resourceGroups/rgName/providers/Microsoft.ContainerService/managedClusters/resourceAKS")]
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

	[TestMethod]
	[DataRow("/subscriptions/39983c36-2aa4-4c7e-907a-6dbd95860295/resourcegroups/a/b/c", "39983c36-2aa4-4c7e-907a-6dbd95860295")]
    [DataRow("/subscriptions/39983c36-2aa4-4c7e-907a-6dbd95860295/resourcegroups", "39983c36-2aa4-4c7e-907a-6dbd95860295")]
    [DataRow("/subscriptions/39983c36-2aa4-4c7e-907a-6dbd95860295", "39983c36-2aa4-4c7e-907a-6dbd95860295")]
    public void GetSubscriptionId(string input, string expectedOutput)
	{
		var result = input.GetSubscriptionId();
		result.Should().Be(expectedOutput);
	}

    [TestMethod]
	[DataRow("activity-123_ok", "activity-123_ok")]
	[DataRow("activity!@#$id", "activityid")]
	[DataRow("activity id !@#$%^&*()", "activityid")]
	public void ToSanitizedInstanceId(string input, string expectedOutput)
	{
		var result = input.ToSanitizedInstanceId();

		result.Should().Be(expectedOutput);
	}
}
