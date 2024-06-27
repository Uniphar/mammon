namespace Mammon.Models.CostManagement
{
	public record SQLDatabaseUsageResponseItem
	{
		public required double DTUAverage { get; set; }
		public required string ResourceId { get; set; }
	}
}
