namespace Mammon.Actors;

public interface ISubscriptionActor : IActor
{
    Task RunWorkload(CostReportRequest request);
}
