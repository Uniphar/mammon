namespace Mammon.Actors;

public class SQLPoolActor(ActorHost actorHost, CostCentreRuleEngine costCentreRuleEngine, StateManagerService stateManager, ILogger<SQLPoolActor> logger)
	: ActorBase<CoreResourceActorState>(actorHost, stateManager), ISQLPoolActor
{
	private const string CostStateName = "sqlPoolActorState";

	public async Task SplitCost(SplittableResourceRequest request, IEnumerable<SQLDatabaseUsageResponseItem> data)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(request);

			var totalCost = request.Resource.Cost;
			var resourceId = request.Resource.ResourceId;
			var tags = request.Resource.Tags;
			var reportId = request.ReportRequest.ReportId;

			var state = await GetStateAsync(CostStateName);
			state.TotalCost = totalCost;
			await SaveStateAsync(CostStateName, state);

			var totalDTU = data.Sum(x => x.DTUAverage);			

			if (totalDTU > 0)
			{
				Dictionary<string, ResourceCost> nsMetrics = [];

				foreach (var db in data)
				{
					var cost = new ResourceCost((decimal)(db.DTUAverage / totalDTU) * totalCost.Cost, totalCost.Currency);
			
					ResourceIdentifier dbRID = new(db.ResourceId);

					var costCentre = costCentreRuleEngine.GetCostCentreForSQLDatabase(dbRID.Name);
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
					await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, nsMetric.Key), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, nsMetric.Value));
				}
			}
			else
			{
				//no usage, assign to sql pool cost centre - likely a default one
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentreRuleEngine.FindCostCentre(resourceId, tags)), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, totalCost));
			}

		}
		catch (Exception ex) 
		{
			logger.LogError(ex, $"Failure in SQLPoolActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}
}
