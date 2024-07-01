namespace Mammon.Models.Workflows;

public record SplittableResourceRequest
{
	public required CostReportRequest ReportRequest { get; set; }
	public required ResourceCostResponse Resource { get; set; }
}
