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
					Request = request,
					Data = usage
				});
		}
		else
		{
			ResourceIdentifier rId = new(request.Resource.ResourceId);

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [request.Resource], SubscriptionId = request.ReportRequest.SubscriptionId },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(AKSVMSSWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}".ToSanitizedInstanceId() });
		}

		return true;
	}
}
