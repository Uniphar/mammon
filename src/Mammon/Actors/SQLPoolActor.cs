using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;

namespace Mammon.Actors;

public class SQLPoolActor(ActorHost actorHost, CostCentreRuleEngine costCentreRuleEngine, ILogger<SQLPoolActor> logger) : ActorBase<CoreResourceActorState>(actorHost), ISQLPoolActor
{
	private const string CostStateName = "sqlPoolCentreState";

	public async Task SplitCost(string reportId, string resourceId, ResourceCost totalCost, IEnumerable<SQLDatabaseUsageResponseItem> data)
	{
		try
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
			ArgumentException.ThrowIfNullOrWhiteSpace(reportId);
			ArgumentNullException.ThrowIfNull(data);
			ArgumentNullException.ThrowIfNull(totalCost);

			var state = await GetStateAsync(CostStateName);

			state.TotalCost = totalCost;

			await SaveStateAsync(CostStateName, state);

			var totalDTU = data.Sum(x => x.DTUAverage);

			decimal allocatedCost= 0; 
			foreach (var db in data)
			{
				var cost = new ResourceCost((decimal)(db.DTUAverage / totalDTU) * totalCost.Cost, totalCost.Currency);
				allocatedCost+= cost.Cost;

				ResourceIdentifier dbRID = new(db.ResourceId);

				var costCentre = costCentreRuleEngine.GetCostCentreForSQLDatabase(dbRID.Name);

				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentre), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, cost));
			}

			if (allocatedCost != totalCost.Cost)
			{
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, costCentreRuleEngine.DefaultCostCentre), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, new(totalCost.Cost-allocatedCost, totalCost.Currency)));
			}

		}
		catch (Exception ex) 
		{
			logger.LogError(ex, $"Failure in SQLPoolActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}
}
