namespace Mammon.Models.Workflows;

public record TenantWorkflowRequest
{
    public required IEnumerable<string> Subscriptions { get; set; }
    public required CostReportRequest ReportRequest { get; set; }
}
