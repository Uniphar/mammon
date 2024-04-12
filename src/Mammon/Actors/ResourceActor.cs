using Dapr.Actors.Runtime;
using MammonActors.Models.Actors;

namespace Mammon.Actors
{
    public class ResourceActor(ActorHost host) : Actor(host), IResourceActor
    {
        public const string CostStateName = "costState";

        public async Task AddCostAsync(double cost, string[]? tags)
        {
            var stateAttempt = await StateManager.TryGetStateAsync<ResourceActorState>(CostStateName);
            var state = (!stateAttempt.HasValue) ? new ResourceActorState() : stateAttempt.Value;

            state.Cost += cost; //actor runtime makes this atomic
            await StateManager.SetStateAsync(CostStateName, state);
        }
    }
}
