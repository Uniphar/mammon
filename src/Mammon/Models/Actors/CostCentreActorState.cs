namespace Mammon.Models.Actors;

public record CostCentreActorState
{
    public double TotalCost { get; set; }
    public Dictionary<string, double>? ResourceCosts { get; set; }
}
