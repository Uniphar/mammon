namespace Mammon.Models.Workflows.Activities.LogAnalytics;

public record IdentifyMissingLAWorkspaceReferencesRequest
{
    public required IEnumerable<LAWorkspaceQueryResponseItem> Data { get; set; }
    public required string ReportId { get; set; }
}
