namespace Mammon.Workflows.Activities.LogAnalytics;

public class SplitLAWorkspaceCostsActivity : WorkflowActivity<SplitLAWorkspaceCostsActivityRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitLAWorkspaceCostsActivityRequest request)
	{
		ResourceIdentifier rId = new(request.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ILAWorkspaceActor>(LAWorkspaceActor.GetActorId(request.ReportId, rId.Name, rId.SubscriptionId!), nameof(LAWorkspaceActor), async (p) => await p.SplitCost(request.ReportId, request.ResourceId, request.TotalWorkspaceCost, request.Data ));

		return true;
	}
}
