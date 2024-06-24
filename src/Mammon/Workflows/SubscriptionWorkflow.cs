namespace Mammon.Workflows;

public class SubscriptionWorkflow : Workflow<CostReportSubscriptionRequest, bool>
{
    public async override Task<bool> RunAsync(WorkflowContext context, CostReportSubscriptionRequest input)
    {
		//obtain cost items from Cost API
		var costs = await context.CallActivityAsync<AzureCostResponse>(nameof(ObtainCostsActivity), input);

		//splittable resources are processed separately
		var rgGroups = costs
			.Where(x => !x.IsSplittable())
			.GroupBy(x =>
			x.ResourceIdentifier.ResourceGroupName
		);

		foreach (var group in rgGroups)
		{
			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = input.ReportRequest.ReportId, Resources = group },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(GroupSubWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{group.Key}" });
		}

		//Log Analytics workspace splitting
		var laWorkspaces = costs.Where(x => x.IsLogAnalyticsWorkspace());
		foreach (var laWorkspace in laWorkspaces)
		{
			await context.CallChildWorkflowAsync<bool>(nameof(LAWorkspaceWorkflow), new LAWorkspaceWorkflowRequest
			{
				LAResourceId = laWorkspace.ResourceId,
				ReportRequest = input.ReportRequest,
				TotalCost = laWorkspace.Cost
			}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{laWorkspace.ResourceIdentifier.Name}" });
		}

		//AKS VMSS splitting
		var aksScaleSets = costs.Where(x => x.IsAKSVMSS());
		foreach (var aksScaleSet in aksScaleSets)
		{
			await context.CallChildWorkflowAsync<bool>(nameof(AKSVMSSWorkflow), new AKSVMSSWorkflowRequest
			{
                VMSSResourceId = aksScaleSet.ResourceId,
			    ReportRequest = input.ReportRequest,
                TotalCost = aksScaleSet.Cost
			}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(AKSVMSSWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{aksScaleSet.ResourceIdentifier.Name}" });
		}

		return true;
    }
}
