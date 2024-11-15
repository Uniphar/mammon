namespace Mammon.Workflows.MySQL;

public class MySQLWorkflow: Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{
		(var usageData, bool usageDataAvailable) = await context.CallActivityAsync<(IEnumerable<MySQLObtainUsageDataActivity> usageData, bool usageDataAvailable)>(nameof(MySQLObtainUsageDataActivity),
			request);

		return true;
	}
}
