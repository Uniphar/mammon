namespace Mammon.Workflows.VisualStudioSubscriptions;

public class SplitVisualStudioSubscriptionsCostsWorkflow : Workflow<VisualStudioSubscriptionsSplittableResourceRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowContext context, VisualStudioSubscriptionsSplittableResourceRequest input)
    {
        return await context.CallActivityAsync<bool>(nameof(SplitVisualStudioSubscriptionsCostsActivity), input);
    }
}
