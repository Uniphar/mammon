namespace Mammon.Models.Actors;

public record CostCentreActorState
{
    public ResourceCost? TotalCost { get; set; }
    public Dictionary<string, ResourceCost>? ResourceCosts { get; set; }
}
