namespace Mammon.Workflows;

public class GroupSubWorkflow : Workflow<GroupSubWorkflowRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowContext context, GroupSubWorkflowRequest input)
    {
        ArgumentNullException.ThrowIfNull(nameof(input));

        Dictionary<string, string> resourceActors = [];

        //assign them to target resource and aggregate costs for each given resource
        foreach (var cost in input.Resources)
        {
            var result = await context.CallActivityAsync<CallResourceActorActivityResponse>(nameof(CallResourceActorActivity),
                new CallResourceActorActivityRequest { ReportId = input.ReportId, Cost = cost });

            resourceActors.TryAdd(result.ResourceActorId, result.ResourceId);
		}

        //with aggregated costs, assign cost centres
        foreach (var resourceActor in resourceActors.Distinct())
        {
            await context.CallActivityAsync<bool>(nameof(AssignCostCentreActivity),
                new AssignCostCentreActivityRequest 
                { 
                    ReportId = input.ReportId, 
                    ResourceActorId = resourceActor.Key, 
                    ResourceId = resourceActor.Value 
                });
        }

        return true;
    }
}
