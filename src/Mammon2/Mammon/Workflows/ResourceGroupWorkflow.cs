using Dapr.Workflow;
using Mammon.DTO.Workflows;

namespace Mammon.Workflows
{
    internal class ResourceGroupWorkflow : Workflow<ResourceGroupWorkflowRequest, ResourceGroupWorkflowResult>
    {
        public async override Task<ResourceGroupWorkflowResult> RunAsync(WorkflowContext context, ResourceGroupWorkflowRequest input)
        {
            Console.WriteLine($"ResourceGroupWorkflow#{context.InstanceId} IN/OUT");
            return new ResourceGroupWorkflowResult();
        }
    }
}
