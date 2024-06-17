namespace Mammon.Models.Workflows.Activities;

public record ExecuteLAWorkspaceDataQueryActivityRequest
{
	public required string LAResourceId { get; set; }
	public required DateTime FromDateTime { get; set; }
	public required DateTime ToDateTime { get; set; }
}
