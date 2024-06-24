namespace Mammon.Actors;

public class ResourceActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine, ILogger<ResourceActor> logger) : ActorBase<ResourceActorState>(host), IResourceActor
{
	public const string CostStateName = "resourceCostState";

	public async Task AddCostAsync(string fullCostId, ResourceCost cost, string parentResourceId, Dictionary<string, string> tags)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(fullCostId);

			var state = await GetStateAsync(CostStateName);

			state.ResourceId = parentResourceId;
			state.Tags = tags;

			state.CostItems ??= [];

			state.CostItems.TryAdd(fullCostId, cost);
			state.TotalCost = new ResourceCost(state.CostItems.Values);

			await SaveStateAsync(CostStateName, state);
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
			var state = await GetStateAsync(CostStateName);

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
	public async Task<(string? costCentre, bool assignmentExists)> GetAssignedCostCentre()
	{
		try
		{
			var state = await GetStateAsync(CostStateName);

			return (state.CostCentre, string.IsNullOrWhiteSpace(state.CostCentre));
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in ResourceActor.GetAssignedCostCentre (ActorId:{Id})");
			throw;
		}
	}
}
