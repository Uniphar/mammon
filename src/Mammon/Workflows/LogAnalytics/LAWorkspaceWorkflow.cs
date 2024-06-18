namespace Mammon.Workflows.LogAnalytics;

public class LAWorkspaceWorkflow : Workflow<LAWorkspaceWorkflowRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, LAWorkspaceWorkflowRequest request)
	{
		//get data per resource/namespace from workspace
		var usage = await context.CallActivityAsync<(IEnumerable<LAWorkspaceQueryResponseItem> elements, bool workspaceFound)>(nameof(ExecuteLAWorkspaceDataQueryActivity),
			 new ExecuteLAWorkspaceDataQueryActivityRequest 
			 {
				 FromDateTime = request.ReportRequest.CostFrom,
				 ToDateTime = request.ReportRequest.CostTo,
				 LAResourceId = request.LAResourceId
			 });

		var rId = new ResourceIdentifier(request.LAResourceId);

		if (usage.workspaceFound)
		{
			var data = usage.elements;
			
			foreach (var item in data)
			{
				item.Selector = item.Selector.ToParentResourceId();
			}

			var gaps = await context.CallActivityAsync<IEnumerable<string>>(nameof(IdenfityLAWorkspaceRefGapsActivity),
				new IdentifyMissingLAWorkspaceReferencesRequest
				{
					ReportId = request.ReportRequest.ReportId,
					Data = data
				});

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
					new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = gaps.Select(i => new ResourceCostResponse { ResourceId = i, Cost = new(0, request.TotalWorkspaceCost.Currency), Tags = [] }) },
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });

			await context.CallActivityAsync<bool>(nameof(SplitLAWorkspaceCostsActivity),
				new SplitLAWorkspaceCostsActivityRequest
				{
					ReportId = request.ReportRequest.ReportId,
					Data = data,
					ResourceId = request.LAResourceId,
					TotalWorkspaceCost = request.TotalWorkspaceCost
				});
		}
		else
		{
			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [new ResourceCostResponse { Cost = request.TotalWorkspaceCost, ResourceId = request.LAResourceId, Tags = [] }] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });
		}

		return true;
	}
}