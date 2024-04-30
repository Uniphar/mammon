namespace Mammon.Models.CostManagement;

public class AzureCostResponse : List<ResourceCost>
{
}

public class ResourceCost
{
    public string ResourceId { get; set; } = string.Empty;
    public double Cost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = [];
}
