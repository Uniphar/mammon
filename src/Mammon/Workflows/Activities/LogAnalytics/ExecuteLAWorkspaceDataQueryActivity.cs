namespace Mammon.Workflows.Activities.LogAnalytics;

public class ExecuteLAWorkspaceDataQueryActivity(
	LogAnalyticsService logAnalyticsService,
	ILogger<ExecuteLAWorkspaceDataQueryActivity> logger): WorkflowActivity<SplittableResourceRequest, (IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)>
{
	public override async Task<(IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		try
		{
            return await logAnalyticsService.CollectUsageData(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
        }
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing Log Analytics Workspace data query for ReportId: {ReportId}, ResourceId: {ResourceId}", request.ReportRequest.ReportId, request.Resource.ResourceId);
            throw;
		}
	}
}
