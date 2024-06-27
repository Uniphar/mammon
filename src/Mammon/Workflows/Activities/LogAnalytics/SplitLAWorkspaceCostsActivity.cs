namespace Mammon.Workflows.Activities.LogAnalytics;

public class SplitLAWorkspaceCostsActivity : WorkflowActivity<SplitUsageActivityRequest<LAWorkspaceQueryResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<LAWorkspaceQueryResponseItem> request)
	{
		ResourceIdentifier rId = new(request.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ILAWorkspaceActor>(LAWorkspaceActor.GetActorId(request.ReportId, rId.Name, rId.SubscriptionId!), nameof(LAWorkspaceActor), async (p) => await p.SplitCost(request.ReportId, request.ResourceId, request.TotalCost, request.Data ));

		return true;
	}
}
