namespace Mammon.Actors;

public class CostCentreActor(ActorHost host, StateManagerService stateManager, ILogger<CostCentreActor> logger) : ActorBase<CostCentreActorState>(host, stateManager), ICostCentreActor
{
    private const string CostStateName = "CostCentreActorState";

    public static string GetActorId(string reportId, string costCentreName) => $"{reportId}_{costCentreName}";

    /// <inheritdoc/>
    public async Task AddCostAsync(string resourceId, ResourceCost cost)
    {
        try
        {
            var state = await GetStateAsync(CostStateName);
            
            state.ResourceCosts ??= [];

            if (!state.ResourceCosts.TryAdd(resourceId, cost))
            {
                //log this - this is either logical error or dapr retrying actor call
                logger.LogWarning($"Resource {resourceId} already exists in cost centre {Id}");
            }

			state.TotalCost = new ResourceCost(state.ResourceCosts.Values);			

            await SaveStateAsync(CostStateName, state);
           
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in CostCentreActor.AddCost (ActorId:{Id})");
            throw;
        }
    }

	public async Task<CostCentreActorState> GetCostsAsync()
	{
        return await GetStateAsync(CostStateName);
	}	
}
