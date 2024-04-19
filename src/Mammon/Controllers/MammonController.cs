using Mammon.Workflows;

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

        await workflowClient.ScheduleNewWorkflowAsync("SubscriptionWorkflow", @event.Data.ReportId, @event.Data);
        
        return Ok();
    }
}