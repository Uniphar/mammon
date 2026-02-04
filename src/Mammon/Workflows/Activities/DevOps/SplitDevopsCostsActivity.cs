namespace Mammon.Workflows.Activities.DevOps;

public class SplitDevopsCostsActivity(ILogger<SplitDevopsCostsActivity> logger) : WorkflowActivity<DevopsResourceRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, DevopsResourceRequest input)
    {
		if (input.DevOpsProjectCosts.IsEmpty()) return false;

		try
		{
            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IDevOpsCostActor>(
                DevOpsCostActor.GetActorId(input.ReportRequest.ReportId, "DevOps", input.ReportRequest.SubscriptionId), nameof(DevOpsCostActor),
                async (p) => await p.SplitCostAsync(input));
            return true;
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error splitting DevOps costs for ReportId: {ReportId}, SubscriptionId: {SubscriptionId}", input.ReportRequest.ReportId, input.ReportRequest.SubscriptionId);
            throw;
		}
    }
}