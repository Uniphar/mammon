namespace Mammon.Models.Workflows.Activities;

public record ObtainCostsActivityRequest
{
	public required string SubscriptionName { get; set; }
	public required GroupingMode GroupingMode { get; set; } = GroupingMode.Resource;
	public required DateTime CostFrom { get; set; }
	public required DateTime CostTo { get; set;}
	public required int PageIndex { get; set; }	 // 0-based
}

public record ObtainDevOpsUsersActivityRequest
{
	public string DevOpsOrganization { get; init; } = string.Empty;
}

public record ObtainDevOpsCostsActivityRequest : ObtainDevOpsUsersActivityRequest
{
	public required string SubscriptionName { get; init; } = string.Empty;
    public required DateTime CostFrom { get; init; }
    public required DateTime CostTo { get; init; }
}

public record ObtainDevOpsProjectCostRequest : ObtainDevOpsUsersActivityRequest
{
	public required ObtainLicensesCostWorkflowResult LicenseCosts { get; init; }
}