namespace Mammon.Workflows;

public class ObtainDevOpsCostWorkflow : Workflow<ObtainDevOpsCostsActivityRequest, ObtainLicensesCostWorkflowResult>
{
    public override async Task<ObtainLicensesCostWorkflowResult> RunAsync(WorkflowContext context, ObtainDevOpsCostsActivityRequest input)
    {
        return await context.CallActivityAsync<ObtainLicensesCostWorkflowResult>(nameof(ObtainDevOpsCostsActivity), input);
    }
}
