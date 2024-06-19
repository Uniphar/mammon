namespace Mammon.Workflows.Activities.AKS;

public class AKSVMSSObtainUsageDataActivity(AKSService aKSService) : WorkflowActivity<AKSVMSSObtainUsageDataActivityRequest, (IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)>
{
	public override async Task<(IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)> RunAsync(WorkflowActivityContext context, AKSVMSSObtainUsageDataActivityRequest request)
	{
		return await aKSService.QueryUsage(request.VMSSResourceId, request.FromDateTime, request.ToDateTime);
	}
}
