namespace Mammon.Actors;

public class MySQLServerActor(ActorHost actorHost, CostCentreRuleEngine costCentreRuleEngine, ILogger<MySQLServerActor> logger) : ActorBase<CoreResourceActorState>(actorHost), IMySQLServerActor
{
	private const string CostStateName = "mySQLServerActorState";

	public async Task SplitCost(SplittableResourceRequest request, IEnumerable<MySQLUsageResponseItem> data)
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

			var totalSize = data.Sum(x => x.DBSize);

			if (totalSize > 0)
			{
				Dictionary<string, ResourceCost> nsMetrics = [];

				foreach (var db in data)
				{
					var cost = new ResourceCost((db.DBSize / totalSize) * totalCost.Cost, totalCost.Currency);				

					var costCentre = costCentreRuleEngine.GetCostCentreForSQLDatabase(db.DBName);
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
