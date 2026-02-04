namespace Mammon.Workflows.Activities.SQLPool;

public class SQLPoolObtainUsageDataActivity(
	SQLPoolService sqlService,
	ILogger<SQLPoolObtainUsageDataActivity> logger) : WorkflowActivity<SplittableResourceRequest, (IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)>
{
	public override async Task<(IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		try
		{
            return await sqlService.ObtainQueryUsage(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
        }
		catch (Exception ex)
		{
			logger.LogError(ex, "Error obtaining SQL Pool usage for ReportId: {ReportId}, ResourceId: {ResourceId}", request.ReportRequest.ReportId, request.Resource.ResourceId);
            throw;
		}
	}
}
