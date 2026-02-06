namespace Mammon.Workflows.Activities.LogAnalytics;

public class IdenfityLAWorkspaceRefGapsActivity(
    CostCentreService costCentreService,
    ILogger<IdenfityLAWorkspaceRefGapsActivity> logger) : WorkflowActivity<IdentifyMissingLAWorkspaceReferencesRequest, IEnumerable<string>>
{
    public override async Task<IEnumerable<string>> RunAsync(WorkflowActivityContext context, IdentifyMissingLAWorkspaceReferencesRequest request)
    {
		try
		{
            var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(request.ReportId, request.SubscriptionId);

            List<string> gaps = [];

            foreach (var resourceId in request.Data
                .Where(d => d.SelectorType == Consts.ResourceIdLAWorkspaceSelectorType)
                .Select(x => x.Selector.ToParentResourceId()).Distinct())
            {
                if (!costCentreStates.Any(c => c.Value?.ResourceCosts != null && c.Value.ResourceCosts.ContainsKey(resourceId)))
                {
                    gaps.Add(resourceId);
                }
            }

            return gaps;
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error identifying Log Analytics Workspace reference gaps for ReportId: {ReportId}, SubscriptionId: {SubscriptionId}", request.ReportId, request.SubscriptionId);
            throw;
		}
    }
}
