namespace Mammon.Workflows.SQLPool;

public class SQLPoolWorkflow : Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{
		(var usageData, bool usageDataAvailable) =  await context.CallActivityAsync<(IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)>(nameof(SQLPoolObtainUsageDataActivity),
			request);

		if (usageDataAvailable)
		{
			await context.CallActivityAsync<bool>(nameof(SQLPoolSplitUsageActivity),
				new SplitUsageActivityRequest<SQLDatabaseUsageResponseItem>
				{
					ReportId = request.ReportRequest.ReportId,
					Data = usageData,
					ResourceId = request.ResourceId,
					TotalCost = request.TotalCost
				});

		}
		else
		{
			ResourceIdentifier rId = new(request.ResourceId);

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [new ResourceCostResponse { Cost = request.TotalCost, ResourceId = request.ResourceId, Tags = [] }] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(SQLPoolWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });
		}

		return true;
	}
}
