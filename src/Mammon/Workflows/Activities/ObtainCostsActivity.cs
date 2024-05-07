namespace Mammon.Workflows.Activities;

public class ObtainCostsActivity(CostManagementService costManagementService) : WorkflowActivity<CostReportSubscriptionRequest, AzureCostResponse>
{
    public override async Task<AzureCostResponse> RunAsync(WorkflowActivityContext context, CostReportSubscriptionRequest request)
    {
        return await costManagementService.QueryForSubAsync(request);
    }
}
