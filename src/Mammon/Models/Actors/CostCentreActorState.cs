namespace Mammon.Models.Actors;

public record CostCentreActorState
{
    public double TotalCost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Dictionary<string, double>? ResourceCosts { get; set; }
}
