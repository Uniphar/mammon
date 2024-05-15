namespace Mammon.Workflows.Activities;

public class ObtainCostsActivity(CostRetrievalService costService) : WorkflowActivity<CostReportSubscriptionRequest, AzureCostResponse>
{
    public override async Task<AzureCostResponse> RunAsync(WorkflowActivityContext context, CostReportSubscriptionRequest request)
    {
        return await costService.QueryForSubAsync(request);
    }
}
