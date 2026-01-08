namespace Mammon.Models.Workflows;

public record VisualStudioSubscriptionsSplittableResourceRequest
{
	public required SubscriptionCostReportRequest ReportRequest { get; set; }
	public required List<VisualStudioSubscriptionCostResponse> VisualStudioSubscriptionCosts { get; set; }
}