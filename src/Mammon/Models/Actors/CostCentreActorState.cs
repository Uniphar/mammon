namespace Mammon.Models.Actors;

public record CostCentreActorState
{
    public  ResourceCost TotalCost { get; set; } = new ResourceCost { Cost = 0, Currency = "N/A" };
    public Dictionary<string, ResourceCost>? ResourceCosts { get; set; }
}
