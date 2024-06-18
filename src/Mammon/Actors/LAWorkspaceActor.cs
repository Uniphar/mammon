using Microsoft.Extensions.Hosting;

namespace Mammon.Actors;

public class LAWorkspaceActor(ActorHost actorHost, ILogger<LAWorkspaceActor> logger, CostCentreService costCentreService, CostCentreRuleEngine costCentreRuleEngine) : Actor(actorHost), ILAWorkspaceActor
{
	private static readonly string CostStateName = "laWorkspaceCostState";
	public static string GetActorId(string reportId, string workspaceName, string subId) => $"{reportId}_{subId}_{workspaceName}";

	public async Task SplitCost(string reportId, string resourceId, ResourceCost laTotalCost, IEnumerable<LAWorkspaceQueryResponseItem> data)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
		ArgumentException.ThrowIfNullOrWhiteSpace(reportId);
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(laTotalCost);

		var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(reportId);

		try
		{
			var state = await GetStateAsync();

			var totalSize = data.Sum(x => x.SizeSum);
			Dictionary<string, ResourceCost> costCentreCosts = [];

			foreach (var item in data)
			{
				string costCentre;
				if (item.SelectorType == Consts.ResourceIdLAWorkspaceSelectorType)
				{
					//map via resource id
					costCentre = costCentreStates.First(x => x.Value?.ResourceCosts != null && x.Value.ResourceCosts.ContainsKey(item.Selector.ToParentResourceId())).Key;
				}
				else
				{
					//map via cost centre namespace mapping
					costCentre = costCentreRuleEngine.GetCostCentreForAKSNamespace(item.Selector);
				}

				if (!costCentreCosts.TryGetValue(costCentre, out var cost))
				{
					cost = new ResourceCost(0, laTotalCost.Currency);
					costCentreCosts.Add(costCentre, cost);
				}

				cost.Cost += ((decimal)item.SizeSum / totalSize) * laTotalCost.Cost;
			}

			foreach (var costCentreCost in costCentreCosts)
			{
				//send them to cost centre actors
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentreCost.Key), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, costCentreCost.Value));
			}


			state.ResourceId = resourceId;
			await SaveStateAsync(state);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in LAWorkspaceActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}

	private async Task<LAWorkspaceActorState> GetStateAsync()
	{
		var stateAttempt = await StateManager.TryGetStateAsync<LAWorkspaceActorState>(CostStateName);
		return (!stateAttempt.HasValue) ? new LAWorkspaceActorState() : stateAttempt.Value;
	}

	private async Task SaveStateAsync(LAWorkspaceActorState state)
	{
		await StateManager.SetStateAsync(CostStateName, state);
	}
}
