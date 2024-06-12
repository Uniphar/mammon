namespace Mammon.Workflows.Activities.LogAnalytics;

public class ExecuteLAWorkspaceDataQueryActivity(LogAnalyticsService logAnalyticsService): WorkflowActivity<ExecuteLAWorkspaceDataQueryActivityRequest, IEnumerable<LAWorkspaceQueryResponseItem>>
{
	public override async Task<IEnumerable<LAWorkspaceQueryResponseItem>> RunAsync(WorkflowActivityContext context, ExecuteLAWorkspaceDataQueryActivityRequest request)
	{
		return await logAnalyticsService.CollectUsageData(request.LAResourceId, request.FromDateTime, request.ToDateTime);
	}
}
