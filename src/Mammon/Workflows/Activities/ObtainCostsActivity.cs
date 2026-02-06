namespace Mammon.Workflows.Activities;

public class ObtainCostsActivity(
    CostRetrievalService costService, 
    CostCentreRuleEngine costCentreRuleEngine,
    ILogger<ObtainCostsActivity> logger) : WorkflowActivity<ObtainCostsActivityRequest, ObtainCostByPageWorkflowResult>
{
    public override async Task<ObtainCostByPageWorkflowResult> RunAsync(WorkflowActivityContext context, ObtainCostsActivityRequest request)
    {
		try
		{
            var result = await costService.QueryResourceCostForSubAsync(request);

            foreach (var item in result)
            {
                costCentreRuleEngine.ProjectModes(item);
            }

            return new ObtainCostByPageWorkflowResult { Costs = result.ToArray(), nextPageAvailable = result.nextPageAvailable };
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error obtaining costs for ReportId: {ReportId}", request.ReportId);
            throw;
		}
    }
}
