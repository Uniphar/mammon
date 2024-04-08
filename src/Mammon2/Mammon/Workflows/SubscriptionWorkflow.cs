using Dapr.Workflow;
using Mammon.DTO.Workflows;

namespace Mammon.Workflows
{
    public class SubscriptionWorkflow : Workflow<SubscriptionWorkflowRequest, SubscriptionWorkflowResult>
    {
      
        public async override Task<SubscriptionWorkflowResult> RunAsync(WorkflowContext context, SubscriptionWorkflowRequest input)
        {
            Console.WriteLine("SubscriptionWorkflow.RunAsync IN");
            List<Task<ResourceGroupWorkflowResult>> childWorflows = new List<Task<ResourceGroupWorkflowResult>>();

            for (int x = 0; x < 5000; x++)
            {
                childWorflows.Add(context.CallChildWorkflowAsync<ResourceGroupWorkflowResult> (nameof(ResourceGroupWorkflow), new ResourceGroupWorkflowRequest(), new ChildWorkflowTaskOptions { InstanceId = $"RGWorkflow{x}" }));
            }

            await Task.WhenAll(childWorflows);


            Console.WriteLine("SubscriptionWorkflow.RunAsync OUT");
            return new SubscriptionWorkflowResult();
        }
    }
  
}
