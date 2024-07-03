namespace Mammon.Workflows.Activities.SQLPool;

public class SQLPoolSplitUsageActivity : WorkflowActivity<SplitUsageActivityRequest<SQLDatabaseUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<SQLDatabaseUsageResponseItem> input)
	{
		ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ISQLPoolActor>(SQLPoolActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(SQLPoolActor),
			async (p) => await p.SplitCost(input.Request, input.Data));

		return true;

	}
}
