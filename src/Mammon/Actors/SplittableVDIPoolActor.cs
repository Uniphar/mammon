namespace Mammon.Actors;

public class SplittableVDIPoolActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine, ILogger<SplittableVDIPoolActor> logger) : ActorBase<CoreResourceActorState>(host), ISplittableVDIPoolActor
{
	private const string CostStateName = "SplittableVDIPoolActorState";

	public async Task<bool> SplitCost(SplittableResourceGroupRequest request, IEnumerable<VDIQueryUsageResponseItem> data)
	{
		ArgumentNullException.ThrowIfNull(request);

		var totalResourceCost = new ResourceCost(request.Resources.Select(x => x.Cost));
		var totalCostDouble = totalResourceCost.Cost;
		var resourceId = request.ResourceGroupId;		
		var reportId = request.ReportRequest.ReportId;

		try 
		{
			var state = await GetStateAsync(CostStateName);
			state.TotalCost = totalResourceCost;
			await SaveStateAsync(CostStateName, state);

			var totalUsage = data.Sum(x => x.SessionCount);

			if (totalUsage > 0)
			{
				Dictionary<string, ResourceCost> nsMetrics = [];

				foreach (var item in data)
				{
					var cost = new ResourceCost((decimal) item.SessionCount / totalUsage * totalResourceCost.Cost, totalResourceCost.Currency);
					var costCentre = costCentreRuleEngine.GetCostCentreForGroupID(item.GroupID);

					if (nsMetrics.TryGetValue(costCentre, out ResourceCost? value))
					{
						value.Cost += cost.Cost;
					}
					else
					{
						value = cost;

						nsMetrics.Add(costCentre, value);
					}
				}

				foreach (var nsMetric in nsMetrics)
				{
					await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
						CostCentreActor.GetActorId(reportId, nsMetric.Key, request.ReportRequest.SubscriptionId),
						nameof(CostCentreActor),
						async (p) => await p.AddCostAsync(resourceId, nsMetric.Value));
				}
			}
			else
			{
				//no usage, assign to RG cost centre - likely a default one
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
					CostCentreActor.GetActorId(reportId, costCentreRuleEngine.FindCostCentre(resourceId, new Dictionary<string, string>()), request.ReportRequest.SubscriptionId),
					nameof(CostCentreActor),
					async (p) => await p.AddCostAsync(resourceId, totalResourceCost));
			}

		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in SplittableVDIPoolActor.SplitCost (ActorId:{Id})");
			throw;
		}

		return true;
	}
}
