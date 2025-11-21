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
                logger.LogWarning($"Unassigned DevOps cost already exists in cost centre {Id}");

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
                    logger.LogWarning($"DevOps Project {project.Key} already exists in cost centre {Id}");
                }
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

    public async Task<CostCentreActorState> GetCostsAsync()
    {
        return await GetStateAsync(CostStateName);
    }

    private static void UpdateTotalCost(CostCentreActorState state)
    {
        if (state.DevOpsProjectCosts == null)
        {
            state.TotalCost = new ResourceCost(state.ResourceCosts.Values);
            return;
        }

        state.TotalCost = new ResourceCost(state.DevOpsProjectCosts.Values
            .SelectMany(dict => dict.Values).Concat(state.ResourceCosts.Values));
    }
}
