namespace Mammon.Workflows;

public class SubscriptionWorkflow : Workflow<CostReportSubscriptionRequest, bool>
{
    public async override Task<bool> RunAsync(WorkflowContext context, CostReportSubscriptionRequest input)
    {
        //obtain cost items from Cost API
        var costs = await context.CallActivityAsync<AzureCostResponse>(nameof(ObtainCostsActivity), input);

        var rgGroups = costs.GroupBy(x =>
            new ResourceIdentifier(x.ResourceId).ResourceGroupName
        );

        foreach ( var group in rgGroups )
        {
            await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow), 
                new GroupSubWorkflowRequest { ReportId = input.ReportRequest.ReportId, Resources = group }, 
                new ChildWorkflowTaskOptions { InstanceId = $"{nameof(GroupSubWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{group.Key}"});
        }

		return true;
    }
}
