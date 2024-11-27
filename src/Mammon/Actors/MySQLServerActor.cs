namespace Mammon.Actors;

//TODO: consider abstract pro rata implementation
public class MySQLServerActor(ActorHost actorHost, CostCentreRuleEngine costCentreRuleEngine, ILogger<MySQLServerActor> logger) : ActorBase<CoreResourceActorState>(actorHost), IMySQLServerActor
{
	private const string CostStateName = "mySQLServerActorState";

	public async Task SplitCost(SplittableResourceRequest request, IDictionary<string, double> proRata)
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

			if (proRata != null && proRata.Count > 0)
			{
				Dictionary<string, ResourceCost> proRataCosts = [];

				foreach (var proRataElement in proRata)
				{
					var cost = new ResourceCost((decimal)(proRataElement.Value / 100) * totalCost.Cost, totalCost.Currency);

					ResourceIdentifier dbRID = new(request.Resource.ResourceId);

					if (proRataCosts.TryGetValue(proRataElement.Key, out ResourceCost? value))
					{
						value.Cost += cost.Cost;
					}
					else
					{
						value = cost;

						proRataCosts.Add(proRataElement.Key, value);
					}
				}

				foreach (var proRataCost in proRataCosts)
				{
					await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, proRataCost.Key), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, proRataCost.Value));
				}
			}
			else
			{
				//no pro rata, assign to mysql server cost centre - likely a default one
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
