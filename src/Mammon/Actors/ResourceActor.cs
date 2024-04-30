namespace Mammon.Actors;

public class ResourceActor(ActorHost host, ILogger<ResourceActor> logger) : Actor(host), IResourceActor
{
    public const string CostStateName = "costState";

    public async Task AddCostAsync(string fullCostId, double cost)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(fullCostId);

            var state = await GetStateAsync(CostStateName);

            state.CostItems ??= new Dictionary<string, double>();

            if (state.CostItems.TryAdd(fullCostId, cost))
                state.Cost += cost;

            await SaveStateAsync(CostStateName, state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in ResourceActor.AddCostAsync (ActorId:{Id})");
            throw;
        }
    }

    public async Task Initialize(string resourceId, Dictionary<string, string> tags)
    {
        try
        {
            var state = await GetStateAsync(CostStateName);

            state.ResourceId = resourceId;
            state.Tags = tags;

            await SaveStateAsync(CostStateName, state);
        }
        catch (Exception ex) 
        {
            logger.LogError(ex, $"Failure in ResourceActor.Initialize (ActorId:{Id})");
        }
    }

    private async Task<ResourceActorState> GetStateAsync(string stateName)
    {
        var stateAttempt = await StateManager.TryGetStateAsync<ResourceActorState>(stateName);
        return (!stateAttempt.HasValue) ? new ResourceActorState() : stateAttempt.Value;
    }

    private async Task SaveStateAsync(string stateName, ResourceActorState state)
    {
        await StateManager.SetStateAsync(stateName, state);
    }
}
