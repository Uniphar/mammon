﻿namespace Mammon.Actors;

public class LAWorkspaceActor(ActorHost actorHost, ILogger<LAWorkspaceActor> logger, CostCentreService costCentreService, CostCentreRuleEngine costCentreRuleEngine) : ActorBase<CoreResourceActorState>(actorHost), ILAWorkspaceActor
{
	private static readonly string CostStateName = "laWorkspaceCostState";

	public async Task SplitCost(string reportId, string resourceId, ResourceCost totalCost, IEnumerable<LAWorkspaceQueryResponseItem> data)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
		ArgumentException.ThrowIfNullOrWhiteSpace(reportId);
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(totalCost);

		var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(reportId);

		try
		{			
			var totalSize = data.Sum(x => x.SizeSum);
			Dictionary<string, ResourceCost> costCentreCosts = [];

			foreach (var item in data)
			{
				string costCentre;
				if (item.SelectorType == Consts.ResourceIdLAWorkspaceSelectorType)
				{
					//map via resource id
					///TODO: consider mapping by simply running the selector through the cost centre rule engine (however no tags at this level)
					///but tags can be retrieved either via resource actor or Azure SDK
					///this removes the need for large payload to be passed around and simplifies the code as no need to identity/process gaps (previous unseen resource ids) first

					costCentre = costCentreStates.First(x => x.Value?.ResourceCosts != null && x.Value.ResourceCosts.ContainsKey(item.Selector.ToParentResourceId())).Key;
				}
				else
				{
					//map via cost centre namespace mapping
					costCentre = costCentreRuleEngine.GetCostCentreForAKSNamespace(item.Selector);
				}

				if (!costCentreCosts.TryGetValue(costCentre, out var cost))
				{
					cost = new ResourceCost(0, totalCost.Currency);
					costCentreCosts.Add(costCentre, cost);
				}

				cost.Cost += ((decimal)item.SizeSum / totalSize) * totalCost.Cost;
			}

			foreach (var costCentreCost in costCentreCosts)
			{
				//send them to cost centre actors
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentreCost.Key), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, costCentreCost.Value));
			}

			var state = await GetStateAsync(CostStateName);

			state.ResourceId = resourceId;
			state.TotalCost = totalCost;

			await SaveStateAsync(CostStateName, state);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in LAWorkspaceActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}	
}
