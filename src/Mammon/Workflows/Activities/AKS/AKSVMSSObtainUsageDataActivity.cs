namespace Mammon.Workflows.Activities.AKS;

public class AKSVMSSObtainUsageDataActivity(AKSService aKSService) : WorkflowActivity<SplittableResourceRequest, (IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)>
{
	public override async Task<(IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		return await aKSService.QueryUsage(request.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
	}
}
