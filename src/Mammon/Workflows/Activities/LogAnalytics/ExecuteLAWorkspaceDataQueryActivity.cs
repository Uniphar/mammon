namespace Mammon.Workflows.Activities.LogAnalytics;

public class ExecuteLAWorkspaceDataQueryActivity(LogAnalyticsService logAnalyticsService): WorkflowActivity<ExecuteLAWorkspaceDataQueryActivityRequest, (IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)>
{
	public override async Task<(IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)> RunAsync(WorkflowActivityContext context, ExecuteLAWorkspaceDataQueryActivityRequest request)
	{
		return await logAnalyticsService.CollectUsageData(request.LAResourceId, request.FromDateTime, request.ToDateTime);
	}
}
