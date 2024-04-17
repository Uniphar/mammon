namespace Mammon;

[Route("api/[controller]")]
[ApiController]
public class MammonController : Controller
{
    [HttpGet()]
    [HttpPost()]
    [Topic("mammon-pub-sub", "ReportRequests")]
    public async Task<StatusCodeResult> Invoke(CloudEvent<CostReportRequest> @event)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));

        CostReportRequest report = @event.Data;

        var subActor = ActorProxy.Create<ISubscriptionActor>(new ActorId(report.SubscriptionName), "SubscriptionActor",
            new ActorProxyOptions { RequestTimeout = Timeout.InfiniteTimeSpan });

        await subActor.RunWorkload(report);
        
        return Ok();
    }
}