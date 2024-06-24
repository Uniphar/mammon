namespace Mammon.Models.Workflows;

public record AKSVMSSWorkflowRequest
{
	public required string VMSSResourceId { get; set; }
	public required CostReportRequest ReportRequest { get; set; }
	public required ResourceCost TotalCost { get; set; }
}
