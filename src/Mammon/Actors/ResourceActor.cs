using Dapr.Actors.Runtime;
using MammonActors.Models.Actors;

namespace Mammon.Actors
{
    public class ResourceActor(ActorHost host) : Actor(host), IResourceActor
    {
        public const string CostStateName = "costState";

        public async Task AddCostAsync(string costId, double cost, string[]? tags)
        {
            ArgumentNullException.ThrowIfNull(costId);

            //TODO: look into setting state TTL
            var stateAttempt = await StateManager.TryGetStateAsync<ResourceActorState>(CostStateName);
            var state = (!stateAttempt.HasValue) ? new ResourceActorState() : stateAttempt.Value;

            if (state.CostItems.TryAdd(costId, cost))
                state.Cost += cost;

            await StateManager.SetStateAsync(CostStateName, state);
        }
    }
}
