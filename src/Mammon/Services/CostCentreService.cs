namespace Mammon.Services;

public class CostCentreService(CostCentreRuleEngine costCentreRuleEngine, IActorProxyFactory actorProxyFactory)
{
	public async Task<Dictionary<string, CostCentreActorState>> RetrieveCostCentreStatesAsync(string reportId, string subscriptionId)
	{
		var costCentres = costCentreRuleEngine.CostCentres;

		Dictionary<string, CostCentreActorState> costCentreStates = [];

		foreach (var costCentre in costCentres)
		{
			var state = await actorProxyFactory.CallActorWithNoTimeout<ICostCentreActor, CostCentreActorState>(
				CostCentreActor.GetActorId(reportId, costCentre, subscriptionId),
				nameof(CostCentreActor),
				async (p) => await p.GetCostsAsync());
			if (state != null)
			{
				costCentreStates.Add(costCentre, state);
			}
		}

		return costCentreStates;
	}
}
