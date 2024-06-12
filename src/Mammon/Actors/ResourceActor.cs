namespace Mammon.Actors;

public class ResourceActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine, ILogger<ResourceActor> logger) : Actor(host), IResourceActor
{
    public const string CostStateName = "resourceCostState";

    public async Task AddCostAsync(string fullCostId, ResourceCost cost, string parentResourceId, Dictionary<string, string> tags)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(fullCostId);

            var state = await GetStateAsync();

            state.ResourceId = parentResourceId;
            state.Tags = tags;            

            state.CostItems ??= [];

            state.CostItems.TryAdd(fullCostId, cost);
            state.TotalCost = new ResourceCost(state.CostItems.Values);

            await SaveStateAsync(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in ResourceActor.AddCostAsync (ActorId:{Id})");
            throw;
        }
    }

    /// <inheritdoc/>    
    public async Task<(string costCentre, ResourceCost cost)> AssignCostCentre()
    {
        try
        {           
            var state = await GetStateAsync();

            var rule = costCentreRuleEngine.FindCostCentreRule(state.ResourceId, state.Tags!);
           
            state.CostCentre = rule.CostCentre;

            return (rule.CostCentre, state.TotalCost);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in ResourceActor.AssignCostCentreCosts (ActorId:{Id})");
            throw;
        }
    }

    /// <inheritdoc/>
	public async Task<(string? costCentre, bool assigned)> GetAssignedCostCentre()
	{
		try
		{
            var state = await GetStateAsync();

            return (state.CostCentre, string.IsNullOrWhiteSpace(state.CostCentre));
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in ResourceActor.GetAssignedCostCentre (ActorId:{Id})");
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
