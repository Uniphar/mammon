namespace Mammon.Workflows.Activities.DevOps;

public class SplitDevopsCostsActivity : WorkflowActivity<DevopsResourceRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, DevopsResourceRequest input)
    {
		if (input.DevOpsProjectCosts.IsEmpty()) return false;

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IDevOpsCostActor>(
			DevOpsCostActor.GetActorId(input.ReportRequest.ReportId, "DevOps", input.ReportRequest.SubscriptionId), nameof(DevOpsCostActor),
			async (p) => await p.SplitCostAsync(input));
		return true;
    }
}