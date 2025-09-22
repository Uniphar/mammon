namespace Mammon.Models.Workflows.Activities.LogAnalytics;

public record IdentifyMissingLAWorkspaceReferencesRequest
{
    public required string SubscriptionId { get; set; }
    public required IEnumerable<LAWorkspaceQueryResponseItem> Data { get; set; }
    public required string ReportId { get; set; }
}
