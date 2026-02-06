namespace Mammon.Workflows.Activities.AKS;

public class AKSVMSSObtainUsageDataActivity(
	AKSService aKSService,
	ILogger<AKSVMSSObtainUsageDataActivity> logger) : WorkflowActivity<SplittableResourceRequest, (IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)>
{
	public override async Task<(IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		try
		{
            return await aKSService.QueryUsage(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
        }
		catch (Exception ex)
		{
			logger.LogError(ex, "Error obtaining AKS VMSS usage data for ResourceId: {ResourceId}", request.Resource.ResourceId);
            throw;
		}
	}
}
