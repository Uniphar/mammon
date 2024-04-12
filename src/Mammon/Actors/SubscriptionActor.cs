using Dapr.Actors.Runtime;
using Mammon.Actors;
using MammonActors.Services;

namespace MammonActors.Actors
{
    public class SubscriptionActor(ActorHost host, CostManagementService costManagementService) : Actor(host), ISubscriptionActor
    {
        private readonly CostManagementService costManagementService = costManagementService;

        public async Task RunWorkload(string payload)
        {
            var costs = await costManagementService.QueryForSubAsync("uniphar-dev");
            int x = 0;
            foreach (var cost in costs) {
                //TODO: implement suitable resource id based actor naming
                var resourceActor = ProxyFactory.CreateActorProxy<IResourceActor>(new Dapr.Actors.ActorId($"ResourceActor{x++}"), "ResourceActor");

                await resourceActor.AddCostAsync(cost.Cost, []);
            }
        }
    }
}
