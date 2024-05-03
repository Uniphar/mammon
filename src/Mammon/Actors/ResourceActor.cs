namespace Mammon.Actors;

public class ResourceActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine, ILogger<ResourceActor> logger) : Actor(host), IResourceActor
{
    public const string CostStateName = "resourceCostState";

    public async Task AddCostAsync(string fullCostId, double cost, string parentResourceId, Dictionary<string, string> tags)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(fullCostId);

            var state = await GetStateAsync();

            state.ResourceId = parentResourceId;
            state.Tags = tags;            

            state.CostItems ??= [];

            if (state.CostItems.TryAdd(fullCostId, cost))
                state.TotalCost += cost;

            await SaveStateAsync(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in ResourceActor.AddCostAsync (ActorId:{Id})");
            throw;
        }
    }

    /// <inheritdoc/>    
    public async Task<IDictionary<string, double>> AssignCostCentreCosts()
    {
        try
        {           
            var state = await GetStateAsync();

            var rule = costCentreRuleEngine.FindCostCentreRule(state.ResourceId, state.Tags!);

            //no splitting yet            
            return new Dictionary<string, double> { { rule.CostCentres.First(), state.TotalCost} };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in ResourceActor.AssignCostCentreCosts (ActorId:{Id})");
            throw;
        }
    }

    private async Task<ResourceActorState> GetStateAsync()
    {
        var stateAttempt = await StateManager.TryGetStateAsync<ResourceActorState>(CostStateName);
        return (!stateAttempt.HasValue) ? new ResourceActorState() : stateAttempt.Value;
    }
    
    private async Task SaveStateAsync(ResourceActorState state)
    {
        await StateManager.SetStateAsync(CostStateName, state);
    }
}
