namespace Mammon.Workflows.Activities;

public class ObtainCostsActivity(CostRetrievalService costService, CostCentreRuleEngine costCentreRuleEngine) : WorkflowActivity<CostReportSubscriptionRequest, AzureCostResponse>
{
    public override async Task<AzureCostResponse> RunAsync(WorkflowActivityContext context, CostReportSubscriptionRequest request)
    {
        var result= await costService.QueryForSubAsync(request);
        
        foreach(var item in result)
        {
            costCentreRuleEngine.ProjectModes(item);
        }

        return result;
    }
}
