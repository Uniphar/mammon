namespace Mammon.Models.CostManagement;

public record SubscriptionDefinition
{
	public required string SubscriptionId { get; set; }
	public required string SubscriptionName { get; set; }
    public required string EnvironmentDesignation { get; set;}
    public string? DevOpsOrganization { get; set; }
}
