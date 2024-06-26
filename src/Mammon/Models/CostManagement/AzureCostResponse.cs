﻿namespace Mammon.Models.CostManagement;

public class AzureCostResponse : List<ResourceCostResponse>
{
	public decimal TotalCost => this.Sum(x => x.Cost.Cost);
}

public class ResourceCostResponse
{
	public required string ResourceId { get; set; }
	public ResourceIdentifier ResourceIdentifier => new(ResourceId);
	public required ResourceCost Cost { get; set; }
	public required Dictionary<string, string> Tags { get; set; }

	public bool IsLogAnalyticsWorkspace() => ResourceIdentifier.IsLogAnalyticsWorkspace();

	public bool IsAKSVMSS() => ResourceIdentifier.ResourceType == "microsoft.compute/virtualmachinescalesets" && Tags.ContainsKey("aks-managed-poolname");

	public bool IsSQLPool() => ResourceIdentifier.ResourceType == "microsoft.Sql/servers/elasticpools";

	public bool IsSplittable() => IsAKSVMSS() || IsLogAnalyticsWorkspace() || IsSQLPool();
}

public record ResourceCost
{
	public ResourceCost() //required for JSON serialization/deserialization
	{

	}

	[SetsRequiredMembers]
	public ResourceCost(decimal costValue, string currency)
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

	public required decimal Cost { get; set; }
	public required string Currency { get; set; }

	public override string ToString()
	{
		return $"{Currency} {Cost:F}";
	}
}
