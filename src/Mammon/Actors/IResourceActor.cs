namespace Mammon.Actors;

public interface IResourceActor : IActor
{
    /// <summary>
    /// add cost to this resource
    /// </summary>
    /// <param name="costId">full resource id associated with the cost</param>
    /// <param name="cost">cost to assign</param>
    /// <param name="parentResourceId">parent resource id - for inner resources</param>
    /// <param name="tags">resource tags</param>
    /// <returns><see cref="Task"/></returns>
    Task AddCostAsync(string costId, ResourceCost cost, string parentResourceId, Dictionary<string, string> tags);

    /// <summary>
    /// determine cost centres and their respective monetary share for this resource's costs
    /// </summary>
    /// <returns>cost centre and their monetary share value mapping</returns>
    Task<IDictionary<string, ResourceCost>> AssignCostCentreCosts();
}
