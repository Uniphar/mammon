using Dapr.Workflow;
using Mammon.DTO.Workflows;
using Mammon.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

var builder = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
{
    services.AddDaprWorkflow(options =>
    {
        // Note that it's also possible to register a lambda function as the workflow
        // or activity implementation instead of a class.
        options.RegisterWorkflow<SubscriptionWorkflow>();
        options.RegisterWorkflow<ResourceGroupWorkflow>();

        // These are the activities that get invoked by the workflow(s).
        //options.RegisterActivity<NotifyActivity>();
        //options.RegisterActivity<ReserveInventoryActivity>();
        //options.RegisterActivity<ProcessPaymentActivity>();
        //options.RegisterActivity<UpdateInventoryActivity>();
    });
});

// Start the app - this is the point where we connect to the Dapr sidecar
using var host = builder.Build();
host.Start();

#if DEBUG

DaprWorkflowClient workflowClient = host.Services.GetRequiredService<DaprWorkflowClient>();

var worflowInstanceId = Guid.NewGuid().ToString();

await workflowClient.ScheduleNewWorkflowAsync(
    name: nameof(SubscriptionWorkflow),
    instanceId: worflowInstanceId,
    input: new SubscriptionWorkflowRequest());

var state = await workflowClient.WaitForWorkflowCompletionAsync(
    instanceId: worflowInstanceId);

Console.WriteLine("I am out");
#endif
