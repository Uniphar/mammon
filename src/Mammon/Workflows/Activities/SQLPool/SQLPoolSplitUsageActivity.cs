namespace Mammon.Workflows.Activities.SQLPool;

public class SQLPoolSplitUsageActivity
	(ILogger<SQLPoolSplitUsageActivity> logger): WorkflowActivity<SplitUsageActivityRequest<SQLDatabaseUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<SQLDatabaseUsageResponseItem> input)
	{
		try
		{
            ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ISQLPoolActor>(SQLPoolActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(SQLPoolActor),
                async (p) => await p.SplitCost(input.Request, input.Data));

            return true;
        }
		catch (Exception ex)
		{
			logger.LogError(ex, "Error splitting SQL Pool usage for ReportId: {ReportId}, ResourceId: {ResourceId}", input.Request.ReportRequest.ReportId, input.Request.Resource.ResourceId);
            throw;
		}
	}
}
