namespace Mammon.Models.Actors;

public record ResourceActorState
{
    public string ResourceId { get; set; } = string.Empty;
    public ResourceCost TotalCost { get; set; } = new ResourceCost(0, string.Empty);
    public Dictionary<string, ResourceCost>? CostItems { get; set; }
    public Dictionary<string, string>? Tags { get; set;  }
    public string? CostCentre { get; set; }
}
