namespace Mammon.Workflows.Activities.VisualStudioSubscriptions;

public class SplitVisualStudioSubscriptionsCostsActivity : WorkflowActivity<VisualStudioSubscriptionsSplittableResourceRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, VisualStudioSubscriptionsSplittableResourceRequest input)
    
    {
        if (input.VisualStudioSubscriptionCosts.Count == 0) return false;

        await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IVisualStudioSubscriptionCostActor>(
            VisualStudioSubscriptionCostActor.GetActorId(input.ReportRequest.ReportId, "VisualStudioSubscriptions", input.ReportRequest.SubscriptionId),
            nameof(VisualStudioSubscriptionCostActor),
            async (p) => await p.SplitCostAsync(input));

        return true;
    }
}
