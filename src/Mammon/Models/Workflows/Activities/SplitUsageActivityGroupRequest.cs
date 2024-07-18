namespace Mammon.Models.Workflows.Activities;

public class SplitUsageActivityGroupRequest<T> where T:class
{
	public required SplittableResourceGroupRequest Request { get; set; }
	public required IEnumerable<T> Data { get; set; }
}
