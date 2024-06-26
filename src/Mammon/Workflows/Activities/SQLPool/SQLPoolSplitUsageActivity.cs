namespace Mammon.Workflows.Activities.SQLPool;

public class SQLPoolSplitUsageActivity : WorkflowActivity<SplitUsageActivityRequest<SQLDatabaseUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<SQLDatabaseUsageResponseItem> request)
	{
		ResourceIdentifier rId = new(request.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ISQLPoolActor>(SQLPoolActor.GetActorId(request.ReportId, rId.Name, rId.SubscriptionId!), nameof(SQLPoolActor), async (p) => await p.SplitCost(request.ReportId, request.ResourceId, request.TotalCost, request.Data));

		return true;

	}
}
