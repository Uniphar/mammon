namespace Mammon.Models.Workflows.Activities;

public record SplitLAWorkspaceCostsActivityRequest
{
	public required string ReportId { get; set; }
	public required string ResourceId { get; set; }
	public required ResourceCost TotalWorkspaceCost { get; set; }
	public required IEnumerable<LAWorkspaceQueryResponseItem> Data { get; set; }
}
