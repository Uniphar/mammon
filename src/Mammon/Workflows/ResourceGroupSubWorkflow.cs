
namespace Mammon.Workflows;

public class ResourceGroupSubWorkflow : Workflow<ResourceGroupSubWorkflowRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowContext context, ResourceGroupSubWorkflowRequest input)
    {
        ArgumentNullException.ThrowIfNull(nameof(input));

        Stack<CallResourceActorActivityResponse> resourceActors = new();

        //assign them to target resource and aggregate costs for each given resource
        foreach (var cost in input.Resources)
        {
            resourceActors.Push(await context.CallActivityAsync<CallResourceActorActivityResponse>(nameof(CallResourceActorActivity),
                new CallResourceActorActivityRequest { ReportId = input.ReportId, Cost = cost }));
        }

        //with aggregated costs, assign cost centres
        foreach (var resourceActor in resourceActors)
        {
            await context.CallActivityAsync<bool>(nameof(AssignCostCentreActivity),
                new AssignCostCentreActivityRequest { ReportId = input.ReportId, ResourceActorId = resourceActor.ResourceActorId, ResourceId = resourceActor.ResourceId });
        }

        return true;
    }
}
