namespace Mammon.Models.Workflows;

public record ResourceGroupSubWorkflowRequest
{
    public required string ReportId { get; set; }
    public required IEnumerable<ResourceCost> Resources { get; set; }
}
