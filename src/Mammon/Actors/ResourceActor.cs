namespace Mammon.Actors;

public class ResourceActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine, ILogger<ResourceActor> logger) : Actor(host), IResourceActor
{
    public const string CostStateName = "resourceCostState";

    public async Task AddCostAsync(string fullCostId, ResourceCost costTuple, string parentResourceId, Dictionary<string, string> tags)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(fullCostId);

            var state = await GetStateAsync();

            state.ResourceId = parentResourceId;
            state.Tags = tags;            

            state.CostItems ??= [];
            state.Currency = costTuple.Currency; //all items are of the same currency

            if (state.CostItems.TryAdd(fullCostId, costTuple.Cost))
                state.TotalCost += costTuple.Cost;

            await SaveStateAsync(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in ResourceActor.AddCostAsync (ActorId:{Id})");
            throw;
        }
    }

    /// <inheritdoc/>    
    public async Task<IDictionary<string, ResourceCost>> AssignCostCentreCosts()
    {
        try
        {           
            var state = await GetStateAsync();

            var rule = costCentreRuleEngine.FindCostCentreRule(state.ResourceId, state.Tags!);

            //pro-rata split if multi cost centre
            if (rule.CostCentres.Length > 1)
            {
                var proRataValue = state.TotalCost/ rule.CostCentres.Length;
                var response = new Dictionary<string, ResourceCost>();
                foreach (var costCentre in rule.CostCentres)
                    response.Add(costCentre, new ResourceCost { Cost = proRataValue, Currency = state.Currency });

                return response;
            }
            else
                return new Dictionary<string, ResourceCost> { { rule.CostCentres.First(), new ResourceCost { Cost = state.TotalCost, Currency = state.Currency } } };
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
