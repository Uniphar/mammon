namespace Mammon.Services;

public interface ICostCentreService
{
    Task<Dictionary<string, CostCentreActorState>> RetrieveCostCentreStatesAsync(string reportId);
}