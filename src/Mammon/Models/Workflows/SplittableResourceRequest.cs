namespace Mammon.Models.Workflows;

public record SplittableResourceRequest
{
	public required string ResourceId { get; set; }
	public required CostReportRequest ReportRequest { get; set; }
	public required ResourceCost TotalCost { get; set; }
}
