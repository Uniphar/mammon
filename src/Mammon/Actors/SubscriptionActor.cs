using Dapr.Actors.Runtime;
using Mammon.Actors;
using Mammon.Extensions;
using Mammon.Models.Actors;
using MammonActors.Services;

namespace MammonActors.Actors
{
    public class SubscriptionActor(ActorHost host, CostManagementService costManagementService) : Actor(host), ISubscriptionActor
    {
        private readonly CostManagementService costManagementService = costManagementService;

        public async Task RunWorkload(CostReportRequest request)
        {
            var costs = await costManagementService.QueryForSubAsync(request);

            Dictionary<string, Guid> details = [];

            foreach (var cost in costs) {

                //derive resource actor id from the full resource id - can contain sub-providers and the string is generally not acceptable to be actor id
                //TODO: consider converting this string into UUID (MD5 based?) - removing need for dictionary later
                var topResourceId = cost.ResourceId.ToResourceActorId();

                if (!details.TryGetValue(topResourceId, out Guid actorIdGuid))
                {
                    actorIdGuid = Guid.NewGuid();
                    details.Add(topResourceId, actorIdGuid);
                }

                //TODO: investigate why guids do not seem to be accepted
                var resourceActor = ProxyFactory.CreateActorProxy<IResourceActor>(new Dapr.Actors.ActorId($"ResourceActor1"), "ResourceActor");

                await resourceActor.AddCostAsync(cost.ResourceId, cost.Cost, []);
            }
        }
    }
}
