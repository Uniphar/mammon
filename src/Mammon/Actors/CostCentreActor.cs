namespace Mammon.Actors;

public class CostCentreActor(ActorHost host, ILogger<CostCentreActor> logger) : ActorBase<CostCentreActorState>(host), ICostCentreActor
{
    private const string CostStateName = "CostCentreActorState";

    public new static string GetActorId(
        string reportId, 
        string costCentreName,
        string subscriptionId) => $"{reportId}_{costCentreName}_{subscriptionId.ToSanitizedInstanceId()}";

    /// <inheritdoc/>
    public async Task AddCostAsync(string resourceId, ResourceCost cost)
    {
        try
        {
            var state = await GetStateAsync(CostStateName);
            
            state.ResourceCosts ??= [];

            if (!state.ResourceCosts.TryAdd(resourceId, cost))
            {
                //log this - this is either logical error or dapr retrying actor call
                logger.LogWarning($"Resource {resourceId} already exists in cost centre {Id}");
            }

            UpdateTotalCost(state);
            await SaveStateAsync(CostStateName, state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in CostCentreActor.AddCost (ActorId:{Id})");
            throw;
        }
    }

    public async Task AddDevOpsUnassignedCostAsync(ResourceCost cost)
    {
        try
        {
            var state = await GetStateAsync(CostStateName);

            state.DevOpsProjectCosts ??= [];

            if (!state.DevOpsProjectCosts.TryAdd("Licenses without active groups", new Dictionary<string, ResourceCost>()
            {
                { "Unassigned ", cost}
            }))
            {
                //log this - this is either logical error or dapr retrying actor call
                logger.LogWarning($"Unassigned Azure DevOps cost already exists in cost centre {Id}");

            }

            UpdateTotalCost(state);
            await SaveStateAsync(CostStateName, state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in CostCentreActor.AddDevOpsUnassignedCostAsync (ActorId:{Id})");
            throw;
        }
    }

    public async Task AddVisualStudioSubscriptionCostAsync(string visualStudioSubscriptionProductName, ResourceCost cost)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(visualStudioSubscriptionProductName);

            var state = await GetStateAsync(CostStateName);
            state.VisualStudioSubscriptionsCosts ??= [];

            if (!state.VisualStudioSubscriptionsCosts.TryAdd(visualStudioSubscriptionProductName, cost))
            {
                //log this - this is either logical error or dapr retrying actor call
                logger.LogWarning($"Visual Studio Subscription cost already exists in cost centre {Id}");
            }
            UpdateTotalCost(state);
            await SaveStateAsync(CostStateName, state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in CostCentreActor.AddVisualStudioSubscriptionCostAsync (ActorId:{Id})");
            throw;
        }
    }

    public async Task AddDevOpsLicenseCostAsync(Dictionary<string, Dictionary<string, ResourceCost>> projectToGroupCosts)
    {
        try
        {
            var state = await GetStateAsync(CostStateName);

            state.DevOpsProjectCosts ??= [];

            foreach(var project in projectToGroupCosts)
            {
                if (!state.DevOpsProjectCosts.TryAdd(project.Key, project.Value))
                {
                    //log this - this is either logical error or dapr retrying actor call
                    logger.LogWarning($"Azure DevOps Project {project.Key} already exists in cost centre {Id}");
                }
            }

            UpdateTotalCost(state);
            await SaveStateAsync(CostStateName, state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in CostCentreActor.AddDevOpsLicenseCostAsync (ActorId:{Id})");
            throw;
        }
    }

    public async Task<CostCentreActorState> GetCostsAsync()
    {
        return await GetStateAsync(CostStateName);
    }

    private static void UpdateTotalCost(CostCentreActorState state)
    {
        ResourceCost? totalCost = null;

        if (state.DevOpsProjectCosts is not null)
        {
            var devOpsCosts = new ResourceCost(state.DevOpsProjectCosts.Values.SelectMany(t => t.Values));
            totalCost = totalCost is null
                ? devOpsCosts
                : new ResourceCost([totalCost, devOpsCosts]);
        }

        if (state.ResourceCosts is not null && state.ResourceCosts.Count > 0)
        {
            var resourceCosts = new ResourceCost(state.ResourceCosts.Values);
            totalCost = totalCost is null
                ? resourceCosts
                : new ResourceCost([totalCost, resourceCosts]);
        }

        if (state.VisualStudioSubscriptionsCosts is not null && state.VisualStudioSubscriptionsCosts.Count > 0)
        {
            var visualStudioSubscriptionsCosts = new ResourceCost(state.VisualStudioSubscriptionsCosts.Values);
            totalCost = totalCost is null 
                ? visualStudioSubscriptionsCosts
                : new ResourceCost([totalCost, visualStudioSubscriptionsCosts]);
        }

        state.TotalCost = totalCost;
    }
}
