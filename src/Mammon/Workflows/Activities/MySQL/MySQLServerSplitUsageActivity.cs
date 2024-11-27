namespace Mammon.Workflows.Activities.MySQL;

public class MySQLServerSplitUsageActivity(CostCentreRuleEngine costCentreRuleEngine) : WorkflowActivity<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		ResourceIdentifier rId = new(request.Resource.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IMySQLServerActor>(MySQLServerActor.GetActorId(request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(MySQLServerActor),
			async (p) => await p.SplitCost(request, costCentreRuleEngine.StaticMySQLMapping));

		return true;
	}
}
