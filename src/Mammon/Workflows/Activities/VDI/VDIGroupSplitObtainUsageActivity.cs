namespace Mammon.Workflows.Activities.VDI;

public class VDIGroupSplitObtainUsageActivity(VDIService vdiService) : WorkflowActivity<SplittableResourceGroupRequest, (List<VDIQueryUsageResponseItem> usageData, bool dataAvailable)>
{
	public override async Task<(List<VDIQueryUsageResponseItem> usageData, bool dataAvailable)> RunAsync(WorkflowActivityContext context, SplittableResourceGroupRequest request)
	{
		return await vdiService.ObtainQueryUsage(request.ResourceGroupId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
	}
}
