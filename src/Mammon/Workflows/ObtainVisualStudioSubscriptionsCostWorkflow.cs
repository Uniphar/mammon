namespace Mammon.Workflows;

public class ObtainVisualStudioSubscriptionsCostWorkflow : Workflow<ObtainVisualStudioSubscriptionCostActivityRequest, List<VisualStudioSubscriptionCostResponse>?>
{
    public override async Task<List<VisualStudioSubscriptionCostResponse>?> RunAsync(WorkflowContext context, ObtainVisualStudioSubscriptionCostActivityRequest input)
    {
        // extra workflow-level retry on top of the HTTP-level Polly retry, for sustained Cost Management throttling (429)
        var options = new WorkflowTaskOptions(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(30),
            backoffCoefficient: 2,
            maxRetryInterval: TimeSpan.FromMinutes(5)));

        return await context.CallActivityAsync<List<VisualStudioSubscriptionCostResponse>?>(nameof(ObtainVisualStudioSubscriptionsCostActivity), input, options);
    }
}