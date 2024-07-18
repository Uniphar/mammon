namespace Mammon.Models.Workflows;

public record SplittableResourceGroupRequest
{
	public required CostReportRequest ReportRequest { get; set; }
	public required IList<ResourceCostResponse> Resources { get; set; }
	public required string ResourceGroupId { get; set; }
}
