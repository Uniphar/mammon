namespace Mammon.Workflows.Activities.SQLPool;

public class SQLPoolObtainUsageDataActivity(SQLPoolService sqlService) : WorkflowActivity<SplittableResourceRequest, (IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)>
{
	public override async Task<(IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		return await sqlService.ObtainQueryUsage(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
	}
}
