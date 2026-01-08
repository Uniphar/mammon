namespace Mammon.Models.Workflows.Activities;

public record ObtainCostsActivityRequest
{
	public required string SubscriptionName { get; set; }
	public required GroupingMode GroupingMode { get; set; } = GroupingMode.Resource;
	public required DateTime CostFrom { get; set; }
	public required DateTime CostTo { get; set;}
	public required int PageIndex { get; set; }	 // 0-based
}

public record ObtainVisualStudioSubscriptionCostActivityRequest
{
	public required string SubscriptionName { get; set; }
    public required DateTime CostFrom { get; set; }
    public required DateTime CostTo { get; set; }
}

public record ObtainDevOpsUsersActivityRequest
{
	public required string DevOpsOrganization { get; init; }
}

public record PaginatedMemberEntitlementsRequest : ObtainDevOpsUsersActivityRequest
{
	public string? ContinuationToken { get; init; } 
}

public record PaginatedMemberEntitlementsResult
{
	public List<MemberEntitlementItem> Users { get; init; } = [];
	public string? ContinuationToken { get; init; }
}

public record ObtainDevOpsCostsActivityRequest : ObtainDevOpsUsersActivityRequest
{
	public required string SubscriptionName { get; init; }
    public required DateTime CostFrom { get; init; }
    public required DateTime CostTo { get; init; }
}

public record ObtainDevOpsProjectCostRequest : ObtainDevOpsUsersActivityRequest
{
	public required string ReportId { get; init; }
	public required ObtainLicensesCostWorkflowResult LicenseCosts { get; init; }
}

public record DevOpsMapProjectCostsActivityRequest : ObtainDevOpsProjectCostRequest
{
	public required List<MemberEntitlementItem> MemberEntitlements { get; init; } = [];
}