namespace Mammon.Workflows.Activities;

public class ObtainCostsActivity(CostRetrievalService costService, CostCentreRuleEngine costCentreRuleEngine) : WorkflowActivity<ObtainCostsActivityRequest, ObtainCostByPageWorkflowResult>
{
    public override async Task<ObtainCostByPageWorkflowResult> RunAsync(WorkflowActivityContext context, ObtainCostsActivityRequest request)
    {
        var result= await costService.QueryForSubAsync(request);
        
        foreach(var item in result)
        {
            costCentreRuleEngine.ProjectModes(item);
        }

        return new ObtainCostByPageWorkflowResult { Costs = result.ToArray(), nextPageAvailable = result.nextPageAvailable};
    }
}
