namespace Mammon.Workflows.Activities.LogAnalytics;

public class IdenfityLAWorkspaceRefGapsActivity(CostCentreService costCentreService) : WorkflowActivity<IdentifyMissingLAWorkspaceReferencesRequest, List<string>>
{
    public override async Task<List<string>> RunAsync(WorkflowActivityContext context, IdentifyMissingLAWorkspaceReferencesRequest request)
    {
        var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(request.ReportId);

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
}