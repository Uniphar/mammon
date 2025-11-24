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
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(ObtainCostByPageWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{costRequest.PageIndex}".ToSanitizedInstanceId() });

			costs.AddRange(pageResponse.Costs);

			pageIndex++;
		}
		while (pageResponse.nextPageAvailable);

		//splittable resources are processed separately
		var rgGroups = costs
			.Where(x => !x.IsSplittableAsResource())
			.GroupBy(x => x.ResourceIdentifier.ResourceGroupName);

		foreach (var group in rgGroups)
		{
			//any resource group with splitable resource as a group gets special treatment
			if (group.Any(x => x.IsSplitableVDI()))
			{
				var rgID = group.First().ResourceIdentifier.GetResourceGroupIdentifier();

				await context.CallChildWorkflowAsync<bool>(nameof(VDIWorkflow),
					new SplittableResourceGroupRequest { ReportRequest = SubscriptionCostReportRequest.FromCostReportRequest(input.ReportRequest, input.SubscriptionId), Resources = group.ToList(), ResourceGroupId = rgID.ToString() },
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(VDIWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{rgID.Name}".ToSanitizedInstanceId() });

			}
			else
			{
				await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
					new GroupSubWorkflowRequest { ReportId = input.ReportRequest.ReportId, Resources = group, SubscriptionId = input.SubscriptionId },
					new ChildWorkflowTaskOptions { InstanceId = $"{nameof(GroupSubWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}{group.Key}".ToSanitizedInstanceId() });
			}
		}

		//MySQL splitting
		var mySQLServers = costs.Where(x => x.IsMySQL());
		foreach (var mySQL in mySQLServers)
		{
			await TriggerSplittableWorkflowAsync<MySQLWorkflow>(context, input, mySQL);
		}

		//AKS VMSS splitting
		var aksScaleSets = costs.Where(x => x.IsAKSVMSS());
		foreach (var aksScaleSet in aksScaleSets)
		{
			await TriggerSplittableWorkflowAsync<AKSVMSSWorkflow>(context, input, aksScaleSet);
		}

		//SQL Pool splitting
		var sqlPools = costs.Where(x => x.IsSQLPool());
		foreach (var sqlPool in sqlPools)
		{
			await TriggerSplittableWorkflowAsync<SQLPoolWorkflow>(context, input, sqlPool);
		}

		//Log Analytics workspace splitting
		///note that this resource type splitting should be the last
		///as its usage may contain resources that are also splittable
		///and any further cost assignment would be ignored due to strict deduplication
		var laWorkspaces = costs.Where(x => x.IsLogAnalyticsWorkspace());
		foreach (var laWorkspace in laWorkspaces)
		{
			await TriggerSplittableWorkflowAsync<LAWorkspaceWorkflow>(context, input, laWorkspace);
		}

		if (string.IsNullOrEmpty(input.DevOpsOrganization)) return true;

		// Get total license costs
		var devOpsLicenseCosts = await context.CallChildWorkflowAsync<ObtainLicensesCostWorkflowResult>(
			nameof(ObtainDevOpsCostWorkflow),
            new ObtainDevOpsCostsActivityRequest
            {
                SubscriptionName = input.SubscriptionName,
                CostFrom = input.ReportRequest.CostFrom,
                CostTo = input.ReportRequest.CostTo,
                DevOpsOrganization = input.DevOpsOrganization
            });

		// Get project costs from group contributions
		var projectsCosts = await context.CallChildWorkflowAsync<DevOpsProjectsCosts>(
			nameof(ObtainDevOpsProjectCostWorkflow),
			new ObtainDevOpsProjectCostRequest
			{
				DevOpsOrganization = input.DevOpsOrganization,
				LicenseCosts = devOpsLicenseCosts,
				ReportId = input.ReportRequest.ReportId,
			},
			new ChildWorkflowTaskOptions { InstanceId = $"{nameof(ObtainDevOpsProjectCostWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}".ToSanitizedInstanceId() });

		// Split DevOps costs across cost centres
		await context.CallChildWorkflowAsync<bool>(
			nameof(SplitDevopsCostsWorkflow),
            new DevopsResourceRequest()
            {
                ReportRequest = SubscriptionCostReportRequest.FromCostReportRequest(input.ReportRequest, input.SubscriptionId),
                DevOpsProjectCosts = projectsCosts,
            }, 
			new ChildWorkflowTaskOptions { InstanceId = $"{nameof(SplitDevopsCostsWorkflow)}{input.SubscriptionName}{input.ReportRequest.ReportId}".ToSanitizedInstanceId() });

        return true;
    }

	private static async Task TriggerSplittableWorkflowAsync<T>(WorkflowContext context, CostReportSubscriptionRequest input, ResourceCostResponse resourceToSplit) where T: Workflow<SplittableResourceRequest,bool>
	{
		var workflowTypeName = typeof(T).Name;

		await context.CallChildWorkflowAsync<bool>(workflowTypeName, new SplittableResourceRequest
		{
			Resource = resourceToSplit,
			ReportRequest = SubscriptionCostReportRequest.FromCostReportRequest(input.ReportRequest, input.SubscriptionId)
		}, new ChildWorkflowTaskOptions { InstanceId = $"{workflowTypeName}{input.SubscriptionName}{input.ReportRequest.ReportId}{resourceToSplit.ResourceIdentifier.Name}".ToSanitizedInstanceId() });
	}
}
