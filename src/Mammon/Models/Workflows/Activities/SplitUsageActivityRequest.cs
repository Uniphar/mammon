namespace Mammon.Models.Workflows.Activities
{
	public record SplitUsageActivityRequest<T> where T:class
	{
		public required SplittableResourceRequest Request { get; set; }
		public required IEnumerable<T> Data { get; set; }
	}
}
