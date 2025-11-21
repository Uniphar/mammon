namespace Mammon.Workflows;

public class ObtainDevOpsProjectCostWorkflow : Workflow<ObtainDevOpsProjectCostRequest, DevOpsProjectsCosts>
{
    public override async Task<DevOpsProjectsCosts> RunAsync(WorkflowContext context, ObtainDevOpsProjectCostRequest input)
    {
        return await context.CallActivityAsync<DevOpsProjectsCosts>(nameof(ObtainDevOpsProjectCostActivity), input);
    }
}