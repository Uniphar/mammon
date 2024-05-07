namespace Mammon.Models.Workflows.Activities;

public record CallResourceActorActivityResponse
{
    public required string ResourceId { get; set; }
    public required string ResourceActorId { get; set; }
}
