namespace Mammon.Models.Workflows;

public record TenantWorkflowRequest
{
    public required IEnumerable<string> Subscriptions { get; init; }
    public required CostReportRequest ReportRequest { get; init; }
}
