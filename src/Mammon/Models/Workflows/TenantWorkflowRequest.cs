namespace Mammon.Models.Workflows;

public record TenantWorkflowRequest
{
    public required IEnumerable<string> Subscriptions { get; set; }
    public required string ReportId { get; set; }
    public required DateTime CostFrom { get; set; }
    public required DateTime CostTo { get; set; }
}
