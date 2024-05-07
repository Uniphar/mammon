namespace Mammon.Models.Workflows.Activities;

public record CallResourceActorActivityRequest
{
    public required string ReportId { get; set; }
    public required ResourceCost Cost { get; set; }
}
