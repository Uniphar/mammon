namespace Mammon.Workflows.Activities.VDI;

public class VDIGroupSplitUsageActivity
	(ILogger<VDIGroupSplitUsageActivity> logger): WorkflowActivity<SplitUsageActivityGroupRequest<VDIQueryUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityGroupRequest<VDIQueryUsageResponseItem> input)
	{
		try
		{
            ResourceIdentifier rId = new(input.Request.ResourceGroupId);

            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ISplittableVDIPoolActor>(SplittableVDIPoolActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(SplittableVDIPoolActor),
                async (p) => await p.SplitCost(input.Request, input.Data));

            return true;
        }
		catch (Exception ex)
		{
			logger.LogError(ex, "Error splitting VDI Group usage for ReportId: {ReportId}, ResourceGroupId: {ResourceGroupId}", input.Request.ReportRequest.ReportId, input.Request.ResourceGroupId);
            throw;
		}
	}
}
