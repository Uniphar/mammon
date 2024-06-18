namespace Mammon.Models.Workflows;

public record LAWorkspaceWorkflowRequest
{
	public required string LAResourceId { get; set; }
	public required CostReportRequest ReportRequest { get; set; }
	public required ResourceCost TotalWorkspaceCost { get; set; }
}
