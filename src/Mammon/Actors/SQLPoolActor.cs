namespace Mammon.Actors;

public class SQLPoolActor(ActorHost actorHost, CostCentreRuleEngine costCentreRuleEngine, ILogger<SQLPoolActor> logger) : ActorBase<CoreResourceActorState>(actorHost), ISQLPoolActor
{
	private const string CostStateName = "sqlPoolCentreState";

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
				decimal allocatedCost = 0;

				foreach (var db in data)
				{
					var cost = new ResourceCost((decimal)(db.DTUAverage / totalDTU) * totalCost.Cost, totalCost.Currency);
					allocatedCost += cost.Cost;

					ResourceIdentifier dbRID = new(db.ResourceId);

					var costCentre = costCentreRuleEngine.GetCostCentreForSQLDatabase(dbRID.Name);

					await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentre), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, cost));
				}
			}
			else
			{
				//no usage, assign to sql pool cost centre - likely a default one
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentreRuleEngine.FindCostCentre(resourceId, tags)), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, new ResourceCost(totalCost.Cost, totalCost.Currency)));
			}

		}
		catch (Exception ex) 
		{
			logger.LogError(ex, $"Failure in SQLPoolActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}
}
