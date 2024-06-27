namespace Mammon.Workflows.AKS;

public class AKSVMSSWorkflow : Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{
		(IEnumerable<AKSVMSSUsageResponseItem> usage, bool success) = await context.CallActivityAsync<(IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)>(nameof(AKSVMSSObtainUsageDataActivity),
			request);

		if (success)
		{
			await context.CallActivityAsync<bool>(nameof(AKSSplitUsageCostActivity),
				new SplitUsageActivityRequest<AKSVMSSUsageResponseItem>
				{
					ReportId = request.ReportRequest.ReportId,
					Data = usage,
					ResourceId = request.ResourceId,
					TotalCost = request.TotalCost
				});
		}
		else
		{
			ResourceIdentifier rId = new(request.ResourceId);

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [new ResourceCostResponse { Cost = request.TotalCost, ResourceId = request.ResourceId, Tags = [] }] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(AKSVMSSWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });
		}

		return true;
	}
}
