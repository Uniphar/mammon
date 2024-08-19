namespace Mammon.Models.Workflows.Activities;

public record ObtainCostsActivityRequest
{
	public required string SubscriptionName { get; set; }
	public required GroupingMode GroupingMode { get; set; } = GroupingMode.Resource;
	public required DateTime CostFrom { get; set; }
	public required DateTime CostTo { get; set;}
	public required int PageIndex { get; set; }	 // 0-based
}
