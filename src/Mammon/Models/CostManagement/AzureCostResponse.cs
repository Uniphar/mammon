using System.Diagnostics.CodeAnalysis;

namespace Mammon.Models.CostManagement;

public class AzureCostResponse : List<ResourceCostResponse>
{
	public string Currency => this.FirstOrDefault()?.Cost?.Currency ?? "N/A";
}

public record ResourceCostResponse
{
	public required string ResourceId { get; set; }
	public required ResourceCost Cost { get; set; }
	public required Dictionary<string, string> Tags { get; set; }
}

public record ResourceCost
{
	[SetsRequiredMembers]
	public ResourceCost(double costValue, string currency)
	{
		Cost = costValue;
		Currency = currency;
	}

	[SetsRequiredMembers]
	public ResourceCost(IEnumerable<ResourceCost> costs)
	{
		if (!costs.All(x => x.Currency == costs.First().Currency))
			throw new ArgumentException("All costs must have the same currency");

		Cost = costs.Sum(x => x.Cost);
		Currency = costs.FirstOrDefault()?.Currency ?? "NA";
	}

	public required double Cost { get; set; }
	public required string Currency { get; set; }

	public override string ToString()
	{
		return $"{Currency} {Cost:F}";
	}
}
