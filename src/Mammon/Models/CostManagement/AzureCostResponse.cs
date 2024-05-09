namespace Mammon.Models.CostManagement;

public class AzureCostResponse : List<ResourceCost>
{
}

public record ResourceCost
{
    public required string ResourceId { get; set; }
    public required double Cost { get; set; }
    public required string Currency { get; set; }
    public required Dictionary<string, string> Tags { get; set; }
}
