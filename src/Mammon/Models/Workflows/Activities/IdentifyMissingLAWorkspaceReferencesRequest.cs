namespace Mammon.Models.Workflows.Activities;

public record IdentifyMissingLAWorkspaceReferencesRequest
{
	public required IEnumerable<LAWorkspaceQueryResponseItem> Data { get; set; }
	public required string ReportId { get; set; }
}
