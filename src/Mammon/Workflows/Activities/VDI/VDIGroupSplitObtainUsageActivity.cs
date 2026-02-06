namespace Mammon.Workflows.Activities.VDI;

public class VDIGroupSplitObtainUsageActivity(
    VDIService vdiService,
    ILogger<VDIGroupSplitObtainUsageActivity> logger) 
    : WorkflowActivity<SplittableResourceGroupRequest, (IEnumerable<VDIQueryUsageResponseItem> usageData, bool dataAvailable)>
{
    public override async Task<(IEnumerable<VDIQueryUsageResponseItem> usageData, bool dataAvailable)> RunAsync(WorkflowActivityContext context, SplittableResourceGroupRequest request)
    {
		try
		{
            return await vdiService.ObtainQueryUsage(request.ResourceGroupId, request.ReportRequest.CostFrom, request.ReportRequest.CostTo);
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error obtaining VDI Group usage for ReportId: {ReportId}, ResourceGroupId: {ResourceGroupId}", request.ReportRequest.ReportId, request.ResourceGroupId);
            throw;
		}
    }
}
