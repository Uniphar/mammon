namespace Mammon.Models.Actors;

public class ResourceActorState
{
    public string ResourceId { get; set; } = string.Empty;
    public double Cost { get; set; }
    public IDictionary<string, double>? CostItems { get; set; }
    public IDictionary<string, string>? Tags { get; set;  }
    public IList<string>? CostCentres { get; set; }
}
