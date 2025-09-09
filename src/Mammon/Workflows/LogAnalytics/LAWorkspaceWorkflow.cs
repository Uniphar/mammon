namespace Mammon.Workflows.LogAnalytics;

public class LAWorkspaceWorkflow : Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{
		//get data per resource/namespace from workspace
		var (elements, workspaceFound) = await context.CallActivityAsync<(IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)>(nameof(ExecuteLAWorkspaceDataQueryActivity),
			 request);

		var rId = new ResourceIdentifier(request.Resource.ResourceId);

		if (workspaceFound)
		{			
			foreach (var item in elements)
			{
				item.Selector = item.Selector.ToParentResourceId();
			}

			var gaps = await context.CallActivityAsync<IEnumerable<string>>(nameof(IdenfityLAWorkspaceRefGapsActivity),
				new IdentifyMissingLAWorkspaceReferencesRequest
				{
					ReportId = request.ReportRequest.ReportId,
					Data = elements
				});

			/* these stand outside of cost api, and are assumed to be allocated purely by LA splitting - no tag*/
			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
					new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = gaps.Select(i => new ResourceCostResponse { ResourceId = i, Cost = new(0, request.Resource.Cost.Currency), Tags = []}) },
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}".ToSanitizedInstanceId() });

			await context.CallActivityAsync<bool>(nameof(SplitLAWorkspaceCostsActivity),
				new SplitUsageActivityRequest<LAWorkspaceQueryResponseItem>
				{
					Request = request,
					Data = elements
				});
		}
		else
		{
			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [request.Resource] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}".ToSanitizedInstanceId() });
		}

		return true;
	}
}