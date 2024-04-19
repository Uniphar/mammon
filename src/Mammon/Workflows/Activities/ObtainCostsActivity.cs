namespace Mammon.Workflows.Activities;

public class ObtainCostsActivity(CostManagementService costManagementService) : WorkflowActivity<CostReportRequest, AzureCostResponse>
{
    public override async Task<AzureCostResponse> RunAsync(WorkflowActivityContext context, CostReportRequest request)
    {
        return await costManagementService.QueryForSubAsync(request);
    }
}
