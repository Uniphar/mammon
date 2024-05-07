namespace Mammon.Actors;

public class CostCentreActor(ActorHost host, ILogger<CostCentreActor> logger) : Actor(host), ICostCentreActor
{
    private const string CostStateName = "costCentreState";

    /// <inheritdoc/>
    public async Task AddCostAsync(string resourceId, double cost)
    {
        try
        {
            var state = await GetStateAsync();
            
            state.ResourceCosts ??= [];

            if (state.ResourceCosts.TryAdd(resourceId, cost))
            {
                state.TotalCost += cost;
            }

            await SaveStateAsync(state);
           
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in CostCentreActor.AddCost (ActorId:{Id})");
            throw;
        }
    }

    private async Task<CostCentreActorState> GetStateAsync()
    {
        var stateAttempt = await StateManager.TryGetStateAsync<CostCentreActorState>(CostStateName);
        return (!stateAttempt.HasValue) ? new CostCentreActorState() : stateAttempt.Value;
    }

    private async Task SaveStateAsync(CostCentreActorState state)
    {
        await StateManager.SetStateAsync(CostStateName, state);
    }
}
