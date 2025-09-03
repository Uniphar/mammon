namespace Mammon.Workflows.Activities.AKS;

public class AKSVMSSObtainUsageDataActivity(AKSService aKSService) : WorkflowActivity<SplittableResourceRequest, (List<AKSVMSSUsageResponseItem> usageElements, bool success)>
{
	public override async Task<(List<AKSVMSSUsageResponseItem> usageElements, bool success)> RunAsync(WorkflowActivityContext context, SplittableResourceRequest request)
	{
		return await aKSService.QueryUsage(request.Resource.ResourceId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
	}
}
