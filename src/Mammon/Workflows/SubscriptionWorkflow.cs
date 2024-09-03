namespace Mammon.Workflows;

public class SubscriptionWorkflow : Workflow<CostReportSubscriptionRequest, bool>
{
    public async override Task<bool> RunAsync(WorkflowContext context, CostReportSubscriptionRequest input)
    {
		List<ResourceCostResponse> costs = [];

		ObtainCostByPageWorkflowResult pageResponse;
		int pageIndex = 0;
		do
		{
			//obtain cost items from Cost API
			var costRequest = new ObtainCostsActivityRequest
			{
				CostFrom = input.ReportRequest.CostFrom,
				CostTo = input.ReportRequest.CostTo,
				GroupingMode = input.GroupingMode,
				PageIndex = pageIndex,
				SubscriptionName = input.SubscriptionName
			};

			pageResponse = await context.CallChildWorkflowAsync<ObtainCostByPageWorkflowResult>(nameof(ObtainCostByPageWorkflow),
					costRequest,
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(ObtainCostByPageWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{costRequest.PageIndex}" });

			costs.AddRange(pageResponse.Costs);

			pageIndex++;
		}
		while (pageResponse.nextPageAvailable);

		//splittable resources are processed separately
		var rgGroups = costs
			.Where(x => !x.IsSplittableAsResource())
			.GroupBy(x => x.ResourceIdentifier.ResourceGroupName);
	

		foreach (var chunk in rgGroups.Chunk(10))		
		{
			List<Task> tasks = [];

			foreach (var group in chunk)
			//any resource group with splittable resource as a group gets special treatment
			if (group.Any(x => x.IsSplitableVDI()))
			{
				var rgID = group.First().ResourceIdentifier.GetResourceGroupIdentifier();

				tasks.Add(context.CallChildWorkflowAsync<bool>(nameof(VDIWorkflow),
					new SplittableResourceGroupRequest { ReportRequest = input.ReportRequest, Resources = group.ToList(), ResourceGroupId = rgID.ToString()},
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(VDIWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{rgID.Name}" }));

			}
			else
			{				
				tasks.Add(context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
					new GroupSubWorkflowRequest { ReportId = input.ReportRequest.ReportId, Resources = group },
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(GroupSubWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{group.Key}" }));
			}

			await Task.WhenAll(tasks);
		}
		

		//AKS VMSS splitting
		var aksScaleSets = costs.Where(x => x.IsAKSVMSS());
		foreach (var aksScaleSet in aksScaleSets)
		{
			await context.CallChildWorkflowAsync<bool>(nameof(AKSVMSSWorkflow), new SplittableResourceRequest
			{
				Resource = aksScaleSet,
				ReportRequest = input.ReportRequest,
			}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(AKSVMSSWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{aksScaleSet.ResourceIdentifier.Name}" });
		}

		//SQL Pool splitting
		var sqlPools = costs.Where(x => x.IsSQLPool());
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
		var laWorkspaces = costs.Where(x => x.IsLogAnalyticsWorkspace());
		foreach (var laWorkspace in laWorkspaces)
		{
			await context.CallChildWorkflowAsync<bool>(nameof(LAWorkspaceWorkflow), new SplittableResourceRequest
			{
				Resource = laWorkspace,
				ReportRequest = input.ReportRequest

			}, new ChildWorkflowTaskOptions { InstanceId = $"{nameof(LAWorkspaceWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{laWorkspace.ResourceIdentifier.Name}" });
		}

		return true;
    }
}
