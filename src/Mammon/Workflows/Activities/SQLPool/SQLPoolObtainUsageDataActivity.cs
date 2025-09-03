namespace Mammon.Workflows.Activities.SQLPool;

public class SQLPoolObtainUsageDataActivity(SQLPoolService sqlService) : WorkflowActivity<SplittableResourceRequest, (List<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)>
{
	public override async Task<(List<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		return await sqlService.ObtainQueryUsage(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
	}
}
