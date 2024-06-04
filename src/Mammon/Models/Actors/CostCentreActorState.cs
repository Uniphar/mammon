namespace Mammon.Models.Actors;

public record CostCentreActorState
{
    public  ResourceCost TotalCost { get; set; } = new ResourceCost(0, string.Empty);
    public Dictionary<string, ResourceCost>? ResourceCosts { get; set; }
}
