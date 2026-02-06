namespace Mammon.Workflows.Activities;

public class ObtainVisualStudioSubscriptionsCostActivity(
	CostRetrievalService costService,
	ILogger<ObtainVisualStudioSubscriptionsCostActivity> logger) : WorkflowActivity<ObtainVisualStudioSubscriptionCostActivityRequest, List<VisualStudioSubscriptionCostResponse>?>
{
    public override async Task<List<VisualStudioSubscriptionCostResponse>?> RunAsync(WorkflowActivityContext context, ObtainVisualStudioSubscriptionCostActivityRequest input)
    {
		try
		{
            return await costService.QueryVisualStudioSubscriptionCostForSubAsync(input);
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error obtaining Visual Studio subscriptions cost for ReportId: {ReportId}", input.ReportId);

            throw;
		}
    }
}
