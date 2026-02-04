namespace Mammon.Workflows.Activities.VisualStudioSubscriptions;

public class SplitVisualStudioSubscriptionsCostsActivity
    (ILogger<SplitVisualStudioSubscriptionsCostsActivity> logger): WorkflowActivity<VisualStudioSubscriptionsSplittableResourceRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, VisualStudioSubscriptionsSplittableResourceRequest input)
    
    {
        if (input.VisualStudioSubscriptionCosts.Count == 0) return false;

		try
		{
            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IVisualStudioSubscriptionCostActor>(
                VisualStudioSubscriptionCostActor.GetActorId(input.ReportRequest.ReportId, "VisualStudioSubscriptions", input.ReportRequest.SubscriptionId),
                nameof(VisualStudioSubscriptionCostActor),
                async (p) => await p.SplitCostAsync(input));

            return true;
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error splitting Visual Studio Subscriptions costs for ReportId: {ReportId}, SubscriptionId: {SubscriptionId}", input.ReportRequest.ReportId, input.ReportRequest.SubscriptionId);
            throw;
		}
    }
}
