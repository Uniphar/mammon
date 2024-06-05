namespace Mammon;

[Route("api/[controller]/[Action]")]
[ApiController]
public class MammonController(DaprWorkflowClient workflowClient, CostCentreRuleEngine costCentreRuleEngine, CostCentreReportService costCentreReportService ) : Controller
{	
	[HttpGet()]
    [HttpPost()]
    [Topic("mammon-pub-sub", Consts.MammonServiceBusTopicName)]
    public async Task<StatusCodeResult> Invoke(CloudEvent<CostReportRequest> @event)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        var subscriptions = costCentreRuleEngine.SubscriptionNames;

        //check if workflow exists but in failed state, so we can reset it
        //or start new fresh instance
        //do nothing for currently running instances

        var workflowName = $"{nameof(TenantWorkflow)}_{@event.Data.ReportId}";

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
            var subRequest = new TenantWorkflowRequest
            {
                Subscriptions = subscriptions,
                ReportRequest = @event.Data
            };

            await workflowClient.ScheduleNewWorkflowAsync(nameof(TenantWorkflow), workflowName, subRequest);
        }

        
        return Ok();
    }

	[HttpOptions()]
	public IActionResult Cron()
	{
		return NoContent();
	}

    [HttpGet]
    [HttpPost]
    public async Task Cron([FromServices] DaprClient daprClient)
    {
        await daprClient.PublishEventAsync("mammon-pub-sub", "ReportRequests", costCentreReportService.GenerateDefaultReportRequest());
    }

#if DEBUG

	[HttpGet]
    public async Task<object> GetReport([FromQuery] string reportId)
    {
        (string attachmentUri, string reportBody) = await costCentreReportService.GenerateReportAsync(new CostReportRequest { ReportId = reportId, CostFrom = DateTime.Now, CostTo = DateTime.Now });

        return new { Attachment = attachmentUri, Body = reportBody };
    }

#endif
}