namespace Mammon.Workflows.Activities.AKS;

public class AKSSplitUsageCostActivity : WorkflowActivity<SplitUsageActivityRequest<AKSVMSSUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<AKSVMSSUsageResponseItem> input)
	{
		ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IAKSVMSSActor>(AKSVMSSActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(AKSVMSSActor),
			async (p) => await p.SplitCost(input.Request, input.Data));

		return true;
	}
}