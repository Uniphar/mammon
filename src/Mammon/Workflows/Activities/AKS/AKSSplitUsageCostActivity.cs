namespace Mammon.Workflows.Activities.AKS;

public class AKSSplitUsageCostActivity(ILogger<AKSSplitUsageCostActivity> logger) : WorkflowActivity<SplitUsageActivityRequest<AKSVMSSUsageResponseItem>, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<AKSVMSSUsageResponseItem> input)
	{
		try
		{
            ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IAKSVMSSActor>(AKSVMSSActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(AKSVMSSActor),
                async (p) => await p.SplitCost(input.Request, input.Data));

            return true;
        }
		catch (Exception ex)
		{
			logger.LogError(ex, "Error splitting AKS VMSS usage for ReportId: {ReportId}, ResourceId: {ResourceId}", input.Request.ReportRequest.ReportId, input.Request.Resource.ResourceId);
            throw;
		}
	}
}