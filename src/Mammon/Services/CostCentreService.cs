namespace Mammon.Services;

public class CostCentreService(CostCentreRuleEngine costCentreRuleEngine)
{
    public async Task<Dictionary<string, CostCentreActorState>> RetrieveCostCentreStatesAsync(string reportId)
    {
        var costCentres = costCentreRuleEngine.CostCentres;

        Dictionary<string, CostCentreActorState> costCentreStates = [];

        foreach (var costCentre in costCentres)
        {
            var state = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor, CostCentreActorState>(
                CostCentreActor.GetActorId(reportId, costCentre),
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