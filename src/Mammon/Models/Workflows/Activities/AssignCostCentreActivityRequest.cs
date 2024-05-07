namespace Mammon.Models.Workflows.Activities;

public record AssignCostCentreActivityRequest
{
    public required string ReportId { get; set; }
    public required string ResourceActorId { get; set; }
    public required string ResourceId { get; set; }
}
