namespace Mammon.Workflows;

public class SubscriptionWorkflow : Workflow<CostReportSubscriptionRequest, bool>
{
    public async override Task<bool> RunAsync(WorkflowContext context, CostReportSubscriptionRequest input)
    {
		//obtain cost items from Cost API
		var costs = await context.CallActivityAsync<AzureCostResponse>(nameof(ObtainCostsActivity), input);

		//splittable resources are processed separately
		//var rgGroups = costs
		//	.Where(x => !x.IsSplittable())
		//	.GroupBy(x => x.ResourceIdentifier.ResourceGroupName
		//);

		//foreach (var group in rgGroups)
		//{
		//	await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
		//		new GroupSubWorkflowRequest { ReportId = input.ReportRequest.ReportId, Resources = group },
		//		new ChildWorkflowTaskOptions { InstanceId = $"{nameof(GroupSubWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{group.Key}" });
		//}

		////AKS VMSS splitting
		//var aksScaleSets = costs.Where(x => x.IsAKSVMSS());
		//foreach (var aksScaleSet in aksScaleSets)
		//{
		//	await context.CallChildWorkflowAsync<bool>(nameof(AKSVMSSWorkflow), new SplittableResourceRequest
		//	{
		//		Resource = aksScaleSet,
		//		ReportRequest = input.ReportRequest,
		//	}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(AKSVMSSWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{aksScaleSet.ResourceIdentifier.Name}" });
		//}

		//SQL Pool splitting
		var sqlPools = costs.Where(x => x.IsSQLPool() && x.ResourceId== "/subscriptions/961b7071-1490-44c4-97b7-b4620c73aee5/resourcegroups/datasql-prod/providers/microsoft.sql/servers/uni-datasql-prod-sql/elasticpools/datasql-prod-sql-pool");
		foreach (var sqlPool in sqlPools)
		{
			await context.CallChildWorkflowAsync<bool>(nameof(SQLPoolWorkflow), new SplittableResourceRequest
			{
				Resource = sqlPool,
				ReportRequest = input.ReportRequest
			}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(SQLPoolWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{sqlPool.ResourceIdentifier.Name}" });
		}

		//Log Analytics workspace splitting
		///note that this resource type splitting should be the last
		///as its usage may contain resources that are also splittable
		///and any further cost assignment would be ignored due to strict deduplication
		//var laWorkspaces = costs.Where(x => x.IsLogAnalyticsWorkspace());
		//foreach (var laWorkspace in laWorkspaces)
		//{
		//	await context.CallChildWorkflowAsync<bool>(nameof(LAWorkspaceWorkflow), new SplittableResourceRequest
		//	{
		//		Resource = laWorkspace,
		//		ReportRequest = input.ReportRequest
				
		//	}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{laWorkspace.ResourceIdentifier.Name}" });
		//}

		return true;
    }
}
