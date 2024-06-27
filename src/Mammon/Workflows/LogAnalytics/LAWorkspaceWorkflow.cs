namespace Mammon.Workflows.LogAnalytics;

public class LAWorkspaceWorkflow : Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{
		//get data per resource/namespace from workspace
		var (elements, workspaceFound) = await context.CallActivityAsync<(IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)>(nameof(ExecuteLAWorkspaceDataQueryActivity),
			 request);

		var rId = new ResourceIdentifier(request.ResourceId);

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

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
					new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = gaps.Select(i => new ResourceCostResponse { ResourceId = i, Cost = new(0, request.TotalCost.Currency), Tags = [] }) },
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });

			await context.CallActivityAsync<bool>(nameof(SplitLAWorkspaceCostsActivity),
				new SplitUsageActivityRequest<LAWorkspaceQueryResponseItem>
				{
					ReportId = request.ReportRequest.ReportId,
					Data = elements,
					ResourceId = request.ResourceId,
					TotalCost = request.TotalCost
				});
		}
		else
		{
			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [new ResourceCostResponse { Cost = request.TotalCost, ResourceId = request.ResourceId, Tags = [] }] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });
		}

		return true;
	}
}