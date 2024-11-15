namespace Mammon.Workflows.Activities.MySQL;

public class MySQLObtainUsageDataActivity(MySQLService mySQLService) : WorkflowActivity<SplittableResourceRequest, (IEnumerable<MySQLUsageResponseItem> usageData, bool usageDataAvailable)>
{
	public override async Task<(IEnumerable<MySQLUsageResponseItem> usageData, bool usageDataAvailable)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest input)
	{
		return await mySQLService.ObtainQueryUsage(input.Resource.ResourceId, input.ReportRequest.CostFrom, input.ReportRequest.CostTo);
	}
}
