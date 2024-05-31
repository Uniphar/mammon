namespace Mammon.Models.CostManagement;

public class AzureCostResponse : List<ResourceCostResponse>
{
    public string Currency => this.FirstOrDefault()?.CostTuple?.Currency ?? "N/A";
}

public record ResourceCostResponse
{
    public required string ResourceId { get; set; }
    public required ResourceCost CostTuple { get; set; }
    public required Dictionary<string, string> Tags { get; set; }
}

public record ResourceCost
{
	public required double Cost { get; set; }
	public required string Currency { get; set; }

	public override string ToString()
	{
		return $"{Currency} {Cost:F}";
	}
}
