namespace Mammon.Actors;

public interface ICostCentreActor : IActor
{
    /// <summary>
    /// add cost from a specific resource to this cost centre
    /// </summary>
    /// <param name="resourceId">resource id of the resource that the cost is associated with</param>
    /// <param name="cost">monetary value with currency</param>
    /// <returns></returns>
    Task AddCostAsync(string resourceId, ResourceCost cost);

    Task AddDevOpsLicenseCostAsync(Dictionary<string, Dictionary<string, ResourceCost>> projectToGroupCosts);
    Task AddDevOpsUnassignedCostAsync(ResourceCost cost);
    Task AddVisualStudioSubscriptionCostAsync(ResourceCost cost);

    /// <summary>
    /// get cost centre state associated with this actor instance
    /// </summary>
    /// <returns>cost centre state</returns>
    Task<CostCentreActorState> GetCostsAsync();
}
