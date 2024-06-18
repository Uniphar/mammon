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
    /// determine cost centre
    /// </summary>
    /// <returns>cost centre</returns>
    Task<(string costCentre, ResourceCost cost)> AssignCostCentre();

    /// <summary>
    /// returns the assigned cost centre for this resource and flag indicating if it was assigned
    /// 
    /// actor NOT assigned cost centre would indicate that the particular actor id has not been seen yet
    /// (e.g. perhaps not bearing a direct cost and thus not returned by Cost API)s
    /// </summary>
    /// <returns>tuple with cost centre and assigned flag</returns>
    Task<(string? costCentre, bool assignmentExists)> GetAssignedCostCentre();
}
