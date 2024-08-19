namespace Mammon.Workflows;

public class ObtainCostByPageWorkflow : Workflow<ObtainCostsActivityRequest, ObtainCostByPageWorkflowResult>
{
	public override async Task<ObtainCostByPageWorkflowResult> RunAsync(WorkflowContext context, ObtainCostsActivityRequest input)
	{
		return await context.CallActivityAsync<ObtainCostByPageWorkflowResult>(nameof(ObtainCostsActivity), input);
	}
}
