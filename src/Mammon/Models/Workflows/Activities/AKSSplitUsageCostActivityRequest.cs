namespace Mammon.Models.Workflows.Activities;

public record AKSSplitUsageCostActivityRequest
{
	public required string ReportId { get; set; }
	public required string ResourceId { get; set; }
	public required ResourceCost TotalCost { get; set; }
	public required IEnumerable<AKSVMSSUsageResponseItem> Data { get; set; }
}
