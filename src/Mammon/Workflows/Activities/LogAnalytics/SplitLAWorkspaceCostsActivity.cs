namespace Mammon.Workflows.Activities.LogAnalytics;

public class SplitLAWorkspaceCostsActivity(
    ILogger<SplitLAWorkspaceCostsActivity> logger) : WorkflowActivity<SplitUsageActivityRequest<LAWorkspaceQueryResponseItem>, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<LAWorkspaceQueryResponseItem> input)
    {
		try
		{
            ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ILAWorkspaceActor>(
                LAWorkspaceActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(LAWorkspaceActor),
                async (p) => await p.SplitCost(input.Request, input.Data));

            return true;
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error splitting Log Analytics Workspace usage for ReportId: {ReportId}, ResourceId: {ResourceId}", input.Request.ReportRequest.ReportId, input.Request.Resource.ResourceId);
            throw;
		}
    }
}
