namespace Mammon.Workflows.LogAnalytics;

public class LAWorkspaceWorkflow : Workflow<LAWorkspaceWorkflowRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, LAWorkspaceWorkflowRequest request)
	{
		//get data per resource/namespace from workspace
		var data = await context.CallActivityAsync<IEnumerable<LAWorkspaceQueryResponseItem>>(nameof(ExecuteLAWorkspaceDataQueryActivity),
			 new ExecuteLAWorkspaceDataQueryActivityRequest 
			 {
				 FromDateTime = request.ReportRequest.CostFrom,
				 ToDateTime = request.ReportRequest.CostTo,
				 LAResourceId = request.LAResourceId
			 });

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

		var rId = new ResourceIdentifier(request.LAResourceId);

		await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = gaps.Select(i=> new ResourceCostResponse { ResourceId = i, Cost = new(0, request.TotalWorkspaceCost.Currency), Tags = [] }) },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });

		await context.CallActivityAsync<bool>(nameof(SplitLAWorkspaceCostsActivity),
			new SplitLAWorkspaceCostsActivityRequest
			{
				ReportId = request.ReportRequest.ReportId,
				Data = data,
				ResourceId = request.LAResourceId,
				TotalWorkspaceCost = request.TotalWorkspaceCost
			});

		return true;
	}
}