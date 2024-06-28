namespace Mammon.Workflows.Activities.LogAnalytics;

public class ExecuteLAWorkspaceDataQueryActivity(LogAnalyticsService logAnalyticsService): WorkflowActivity<SplittableResourceRequest, (IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)>
{
	public override async Task<(IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		return await logAnalyticsService.CollectUsageData(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
	}
}
