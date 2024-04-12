using Dapr.Actors;
using Mammon.Models.Actors;

namespace MammonActors.Actors
{
    public interface ISubscriptionActor : IActor
    {
        Task RunWorkload(CostReportRequest request);
    }
}
