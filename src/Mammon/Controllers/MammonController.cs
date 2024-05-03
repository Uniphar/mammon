namespace Mammon;

[Route("api/[controller]")]
[ApiController]
public class MammonController(DaprWorkflowClient workflowClient, CostCentreRuleEngine costCentreRuleEngine) : Controller
{
    [HttpGet()]
    [HttpPost()]
    [Topic("mammon-pub-sub", "ReportRequests")]
    public async Task<StatusCodeResult> Invoke(CloudEvent<CostReportRequest> @event)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        var subscriptions = costCentreRuleEngine.Subscriptions;

        foreach (var subscription in subscriptions)
        {
            //check if workflow exists but in failed state, so we can reset it
            //or start new fresh instance
            //do nothing for currently running instances

            var workflowName = $"SubscriptionWorkflow_{subscription}_{@event.Data.ReportId}";

            WorkflowState? workflowInstance;
            try
            {
                workflowInstance = await workflowClient.GetWorkflowStateAsync(workflowName);
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown)
            {
                workflowInstance = null;
            }

            if (workflowInstance?.RuntimeStatus == WorkflowRuntimeStatus.Failed || workflowInstance?.RuntimeStatus == WorkflowRuntimeStatus.Terminated)
            {
                await workflowClient.PurgeInstanceAsync(workflowName);
                workflowInstance = null;
            }

            if (workflowInstance == null)
            {
                var subRequest = new CostReportSubscriptionRequest
                {
                    ReportId = @event.Data.ReportId,
                    CostFrom = @event.Data.CostFrom,
                    CostTo = @event.Data.CostTo,
                    SubscriptionName = subscription
                };

                await workflowClient.ScheduleNewWorkflowAsync("SubscriptionWorkflow", workflowName, subRequest);
            }
        }
        
        return Ok();
    }
}