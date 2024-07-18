namespace Mammon.Workflows.Activities.VDI;

public class VDIGroupSplitUsageActivity : WorkflowActivity<SplitUsageActivityGroupRequest<VDIQueryUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityGroupRequest<VDIQueryUsageResponseItem> input)
	{
		ResourceIdentifier rId = new(input.Request.ResourceGroupId);

		await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ISplittableVDIPoolActor>(SplittableVDIPoolActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(SplittableVDIPoolActor),
			async (p) => await p.SplitCost(input.Request, input.Data));

		return true;
	}
}
