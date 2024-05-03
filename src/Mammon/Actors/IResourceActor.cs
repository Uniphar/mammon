namespace Mammon.Actors;

public interface IResourceActor : IActor
{
    //Task Initialize(string resourceId, Dictionary<string, string> tags);

    Task AddCostAsync(string costId, double cost, string parentResourceId, Dictionary<string, string> tags);

    /// <summary>
    /// determine cost centres and their respective monetary share for this resource's costs
    /// </summary>
    /// <returns>cost centre and their monetary share value mapping</returns>
    Task<IDictionary<string, double>> AssignCostCentreCosts();
}
