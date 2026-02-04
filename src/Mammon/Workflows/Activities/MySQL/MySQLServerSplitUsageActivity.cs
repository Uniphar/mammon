namespace Mammon.Workflows.Activities.MySQL;

public class MySQLServerSplitUsageActivity(
	CostCentreRuleEngine costCentreRuleEngine,
	ILogger<MySQLServerSplitUsageActivity> logger) : WorkflowActivity<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		try
		{
            ResourceIdentifier rId = new(request.Resource.ResourceId);

            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IMySQLServerActor>(MySQLServerActor.GetActorId(request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(MySQLServerActor),
                async (p) => await p.SplitCost(request, costCentreRuleEngine.StaticMySQLMapping));

            return true;
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error splitting MySQL Server usage for ReportId: {ReportId}, ResourceId: {ResourceId}", request.ReportRequest.ReportId, request.Resource.ResourceId);
            throw;
		}
	}
}
