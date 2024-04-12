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

            ///TODO: these increments do not allow for predictable call retries
            ///it may be better to simply collect records as a list with "fixed" record key so that duplicates can be identified
            ///and aggreated the deduded list at the end           
            state.Cost += cost; //actor runtime makes this atomic
            
            await StateManager.SetStateAsync(CostStateName, state);
        }
    }
}
