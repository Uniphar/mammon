namespace Mammon.Workflows.Activities.LogAnalytics;

public class SplitLAWorkspaceCostsActivity : WorkflowActivity<SplitUsageActivityRequest<LAWorkspaceQueryResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<LAWorkspaceQueryResponseItem> input)
	{
		ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ILAWorkspaceActor>(LAWorkspaceActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(LAWorkspaceActor),
			async (p) => await p.SplitCost(input.Request, input.Data ));

		return true;
	}
}
