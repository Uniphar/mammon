namespace Mammon.Workflows.Activities;

public class ObtainVisualStudioSubscriptionsCostActivity(CostRetrievalService costService) : WorkflowActivity<ObtainVisualStudioSubscriptionCostActivityRequest, List<VisualStudioSubscriptionCostResponse>?>
{
    public override async Task<List<VisualStudioSubscriptionCostResponse>?> RunAsync(WorkflowActivityContext context, ObtainVisualStudioSubscriptionCostActivityRequest input)
    {
        return await costService.QueryVisualStudioLicensesForSubAsync(input);
    }
}
