namespace Mammon.Workflows;

public class SplitDevopsCostsWorkflow : Workflow<DevopsResourceRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowContext context, DevopsResourceRequest input)
    {
        return await context.CallActivityAsync<bool>(nameof(SplitDevopsCostsActivity), input);
    }
}