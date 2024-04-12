using Dapr.Actors;

namespace MammonActors.Actors
{
    public interface ISubscriptionActor : IActor
    {
        Task RunWorkload(string payload);
    }
}
