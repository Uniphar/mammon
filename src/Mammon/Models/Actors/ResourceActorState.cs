namespace Mammon.Models.Actors;

public record ResourceActorState
{
    public string ResourceId { get; set; } = string.Empty;
    public ResourceCost TotalCost { get; set; } = new ResourceCost { Cost = 0, Currency = "N/A" };
    public Dictionary<string, ResourceCost>? CostItems { get; set; }
    public Dictionary<string, string>? Tags { get; set;  }
    public List<string>? CostCentres { get; set; }
}
