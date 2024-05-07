namespace Mammon.Models.Actors;

public record ResourceActorState
{
    public string ResourceId { get; set; } = string.Empty;
    public double TotalCost { get; set; }
    public Dictionary<string, double>? CostItems { get; set; }
    public Dictionary<string, string>? Tags { get; set;  }
    public List<string>? CostCentres { get; set; }
}
