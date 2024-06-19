namespace Mammon.Workflows.Activities.AKS;

public class AKSSplitUsageCostActivity : WorkflowActivity<AKSSplitUsageCostActivityRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, AKSSplitUsageCostActivityRequest request)
	{
		ResourceIdentifier rId = new(request.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IAKSVMSSActor>(AKSVMSSActor.GetActorId(request.ReportId, rId.Name, rId.SubscriptionId!), nameof(AKSVMSSActor), async (p) => await p.SplitCost(request.ReportId, request.ResourceId, request.TotalCost, request.Data));

		return true;
	}
}