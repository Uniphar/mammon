namespace Mammon.Workflows;

public class TenantWorkflow : Workflow<TenantWorkflowRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowContext context, TenantWorkflowRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);

        List<Task<SubscriptionWorkflowResult>> pendingWorkflows = [];

        foreach (var subscription in input.Subscriptions)
        {
            pendingWorkflows.Add(context.CallChildWorkflowAsync<SubscriptionWorkflowResult>(nameof(SubscriptionWorkflow),
                new CostReportSubscriptionRequest
                {
                    ReportId = input.ReportId,
                    CostFrom = input.CostFrom,
                    CostTo = input.CostTo,
                    SubscriptionName = subscription
                },
                new ChildWorkflowTaskOptions { InstanceId = $"{nameof(SubscriptionWorkflow)}{subscription}{input.ReportId}" }));
        }

        await Task.WhenAll(pendingWorkflows);

        return true;
    }
}
