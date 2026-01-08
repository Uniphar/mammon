namespace Mammon.Workflows;

public class ObtainVisualStudioSubscriptionsCostWorkflow : Workflow<ObtainVisualStudioSubscriptionCostActivityRequest, List<VisualStudioSubscriptionCostResponse>?>
{
    public override async Task<List<VisualStudioSubscriptionCostResponse>?> RunAsync(WorkflowContext context, ObtainVisualStudioSubscriptionCostActivityRequest input)
    {
        return await context.CallActivityAsync<List<VisualStudioSubscriptionCostResponse>?>(nameof(ObtainVisualStudioSubscriptionsCostActivity), input);
    }
}