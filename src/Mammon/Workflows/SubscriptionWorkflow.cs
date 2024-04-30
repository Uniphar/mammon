namespace Mammon.Workflows;

public class SubscriptionWorkflow : Workflow<CostReportRequest, SubscriptionWorkflowResult>
{

    public async override Task<SubscriptionWorkflowResult> RunAsync(WorkflowContext context, CostReportRequest input)
    {
        var costs = await context.CallActivityAsync<AzureCostResponse>("ObtainCostsActivity", input);       

        Stack<string> resourceActorIds = new();

        foreach (var cost in costs)
        {            
            resourceActorIds.Push(await context.CallActivityAsync<string>("CallResourceActorActivity", new CallResourceActorActivityRequest { Cost = cost }));            
        }

        return new SubscriptionWorkflowResult();
        
    }
}
