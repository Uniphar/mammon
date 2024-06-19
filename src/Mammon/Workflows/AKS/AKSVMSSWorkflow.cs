namespace Mammon.Workflows.AKS;

public class AKSVMSSWorkflow : Workflow<AKSVMSSWorkflowRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, AKSVMSSWorkflowRequest request)
	{
		(IEnumerable<AKSVMSSUsageResponseItem> usage, bool success) = await context.CallActivityAsync<(IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)>(nameof(AKSVMSSObtainUsageDataActivity),
			new AKSVMSSObtainUsageDataActivityRequest 
			{ 
				VMSSResourceId = request.VMSSResourceId, 
				FromDateTime = request.ReportRequest.CostFrom, 
				ToDateTime = request.ReportRequest.CostTo
			});

		if (success)
		{
			await context.CallActivityAsync<bool>(nameof(AKSSplitUsageCostActivity),
				new AKSSplitUsageCostActivityRequest
				{
					ReportId = request.ReportRequest.ReportId,
					Data = usage,
					ResourceId = request.VMSSResourceId,
					TotalCost = request.TotalCost
				});
		}
		else
		{
			ResourceIdentifier rId = new(request.VMSSResourceId);

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [new ResourceCostResponse { Cost = request.TotalCost, ResourceId = request.VMSSResourceId, Tags = [] }] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(AKSVMSSWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });
		}

		return true;
	}
}
