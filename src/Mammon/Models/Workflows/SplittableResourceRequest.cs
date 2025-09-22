namespace Mammon.Models.Workflows;

public record SplittableResourceRequest
{
	public required SubscriptionCostReportRequest ReportRequest { get; set; }
	public required ResourceCostResponse Resource { get; set; }
}
