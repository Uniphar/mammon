namespace Mammon.Models.Actors;

public record ResourceActorState : CoreResourceActorState
{
    public Dictionary<string, ResourceCost>? CostItems { get; set; }
    public Dictionary<string, string>? Tags { get; set;  }
    public string? CostCentre { get; set; }
}
