namespace Mammon.Actors;

public class DevOpsCostActor(
	ActorHost actorHost, ILogger<DevOpsCostActor> logger, 
	CostCentreService costCentreService, 
	CostCentreRuleEngine costCentreRuleEngine) 
	: ActorBase<CoreResourceActorState>(actorHost), IDevOpsCostActor
{
    public async Task SplitCostAsync(DevopsResourceRequest request)
    {
		ArgumentNullException.ThrowIfNull(request);

		if (request.DevOpsProjectCosts.ProjectCosts.Count == 0)
		{
			logger.LogWarning($"No DevOps costs to split for ReportId:{request.ReportRequest.ReportId}, SubscriptionId:{request.ReportRequest.SubscriptionId}");
			return;
		}

		var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(request.ReportRequest.ReportId, request.ReportRequest.SubscriptionId);

		try
		{

			Dictionary<string, Dictionary<string, Dictionary<string, ResourceCost>>> costCentreCosts = [];

			foreach (var projectCost in request.DevOpsProjectCosts.ProjectCosts)
			{
				string costCentre = costCentreRuleEngine.FindCostCentreForDevopsProject(projectCost.ProjectName);

				if (!costCentreCosts.TryGetValue(costCentre, out var cost))
				{
					cost = new Dictionary<string, Dictionary<string, ResourceCost>>();
					costCentreCosts.Add(costCentre, cost);
				}

				foreach (var projectGroup in projectCost.ContributingGroupCosts)
				{
					if (!cost.TryGetValue(projectCost.ProjectName, out var projectGroupCosts))
					{
						projectGroupCosts = new Dictionary<string, ResourceCost>();
						cost.Add(projectCost.ProjectName, projectGroupCosts);
					}

					cost[projectCost.ProjectName].Add(projectGroup.Key, projectGroup.Value);
				}
			}

			foreach (var costCentreCost in costCentreCosts)
			{
				//send them to cost centre actors
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
					CostCentreActor.GetActorId(request.ReportRequest.ReportId, costCentreCost.Key, request.ReportRequest.SubscriptionId),
					nameof(CostCentreActor),
					async (p) => await p.AddDevOpsLicenseCostAsync(costCentreCost.Value));
			}

            if (request.DevOpsProjectCosts.UnassignedCost is not null)
            {
                var defaultCostCentre = costCentreRuleEngine.DefaultDevOpsCostCentre;

                await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
					CostCentreActor.GetActorId(request.ReportRequest.ReportId, defaultCostCentre, request.ReportRequest.SubscriptionId),
					nameof(CostCentreActor),
					async (p) => await p.AddDevOpsUnassignedCostAsync(request.DevOpsProjectCosts.UnassignedCost));

            }
        }
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in DevOpsCostActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}
}
