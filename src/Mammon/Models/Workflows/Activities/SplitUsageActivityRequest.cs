namespace Mammon.Models.Workflows.Activities
{
	public record SplitUsageActivityRequest<T> where T:class
	{
		public required string ReportId { get; set; }
		public required string ResourceId { get; set; }
		public required ResourceCost TotalCost { get; set; }
		public required IEnumerable<T> Data { get; set; }
	}
}
