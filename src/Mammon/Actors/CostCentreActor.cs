namespace Mammon.Actors;

public class CostCentreActor(ActorHost host, ILogger<CostCentreActor> logger) : ActorBase<CostCentreActorState>(host), ICostCentreActor
{
    private const string CostStateName = "costCentreState";

    public static string GetActorId(string reportId, string costCentreName) => $"{reportId}_{costCentreName}";

    /// <inheritdoc/>
    public async Task AddCostAsync(string resourceId, ResourceCost cost)
    {
        try
        {
            var state = await GetStateAsync(CostStateName);
            
            state.ResourceCosts ??= [];

            state.ResourceCosts.TryAdd(resourceId, cost);
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
