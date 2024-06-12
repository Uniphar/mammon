namespace Mammon.Workflows.Activities.LogAnalytics;

public class IdentityLAWorkspaceRefGapsActivity(CostCentreService costCentreService) : WorkflowActivity<IdentityMissingLAWorkspaceReferencesRequest, IEnumerable<string>>
{
	public override async Task<IEnumerable<string>> RunAsync(WorkflowActivityContext context, IdentityMissingLAWorkspaceReferencesRequest request)
	{
		var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(request.ReportId);

		List<string> gaps = [];

		foreach (var resourceId in request.Data
			.Where(d=>d.SelectorType==Consts.ResourceIdLAWorkspaceSelectorType)
			.Select(x => x.Selector.ToParentResourceId()).Distinct())
		{
			if (!costCentreStates.Any(c=>c.Value?.ResourceCosts!=null && c.Value.ResourceCosts.ContainsKey(resourceId)))
			{
				gaps.Add(resourceId);
			}
		}

		return gaps;
	}
}
