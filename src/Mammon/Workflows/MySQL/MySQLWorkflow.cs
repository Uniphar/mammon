namespace Mammon.Workflows.MySQL;

public class MySQLWorkflow: Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{	
		await context.CallActivityAsync<bool>(nameof(MySQLServerSplitUsageActivity), request);
	
		return true;
	}
}
