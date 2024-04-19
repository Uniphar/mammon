namespace Mammon;

[Route("api/[controller]")]
[ApiController]
public class MammonController(DaprWorkflowClient workflowClient) : Controller
{
    [HttpGet()]
    [HttpPost()]
    [Topic("mammon-pub-sub", "ReportRequests")]
    public async Task<StatusCodeResult> Invoke(CloudEvent<CostReportRequest> @event)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        //check if workflow exists but in failed state, so we can reset it
        //or start new fresh instance
        //do nothing for currently running instances

        WorkflowState? workflowInstance;
        try
        {
            workflowInstance = await workflowClient.GetWorkflowStateAsync(@event.Data.ReportId);
        }
        catch(RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown)
        {
            workflowInstance = null;
        }
    
        if (workflowInstance != null 
            && (workflowInstance.RuntimeStatus==WorkflowRuntimeStatus.Failed || workflowInstance.RuntimeStatus== WorkflowRuntimeStatus.Terminated))
        {
            await workflowClient.PurgeInstanceAsync(@event.Data.ReportId);
            workflowInstance= null;
        }

        if (workflowInstance == null)
        {
            await workflowClient.ScheduleNewWorkflowAsync("SubscriptionWorkflow", @event.Data.ReportId, @event.Data);
        }
        
        return Ok();
    }
}