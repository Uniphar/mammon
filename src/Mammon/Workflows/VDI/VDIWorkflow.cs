namespace Mammon.Workflows.VDI;

public class VDIWorkflow : Workflow<SplittableResourceGroupRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceGroupRequest request)
	{
		(IEnumerable<VDIQueryUsageResponseItem> usageData, bool dataAvailable) = await context.CallActivityAsync<(IEnumerable<VDIQueryUsageResponseItem> usageData, bool dataAvailable)>(nameof(VDIGroupSplitObtainUsageActivity), request);
		
		if (dataAvailable)
		{
			//TODO: consider retroactively spliting cost PER resource using the data above - at resource level rather than RG level (implications to Log Analytics splitting)
			await context.CallActivityAsync<bool>(nameof(VDIGroupSplitUsageActivity),
				new SplitUsageActivityGroupRequest<VDIQueryUsageResponseItem>
				{
					Request = request,
					Data = usageData
				});

		}
		else
		{
			ResourceIdentifier rId = new(request.ResourceGroupId);

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = request.Resources },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(VDIWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}".ToSanitizedInstanceId() });
		}

		return true;
	}
}
