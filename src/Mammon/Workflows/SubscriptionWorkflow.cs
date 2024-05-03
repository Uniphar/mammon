namespace Mammon.Workflows;

public class SubscriptionWorkflow : Workflow<CostReportRequest, SubscriptionWorkflowResult>
{

    public async override Task<SubscriptionWorkflowResult> RunAsync(WorkflowContext context, CostReportRequest input)
    {
        //obtain cost items from Cost API
        var costs = await context.CallActivityAsync<AzureCostResponse>("ObtainCostsActivity", input);

        Stack<CallResourceActorActivityResponse> resourceActors = new();

        //assign them to target resource and aggregate costs for each given resource
        foreach (var cost in costs)
        {
            resourceActors.Push(await context.CallActivityAsync<CallResourceActorActivityResponse>("CallResourceActorActivity", 
                new CallResourceActorActivityRequest {ReportId = input.ReportId, Cost = cost }));
        }

        //with aggregated costs, assign cost centres
        foreach (var resourceActor in resourceActors)
        {
            await context.CallActivityAsync<bool>("AssignCostCentreActivity",
                new AssignCostCentreActivityRequest { ReportId = input.ReportId, ResourceActorId = resourceActor.ResourceActorId, ResourceId = resourceActor.ResourceId });
        }


        return new SubscriptionWorkflowResult();

    }
}
