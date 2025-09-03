namespace Mammon.Workflows.SQLPool;

public class SQLPoolWorkflow : Workflow<SplittableResourceRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, SplittableResourceRequest request)
	{
		(var usageData, bool usageDataAvailable) =  await context.CallActivityAsync<(List<SQLDatabaseUsageResponseItem> usageData, bool usageDataAvailable)>(nameof(SQLPoolObtainUsageDataActivity),
			request);

		if (usageDataAvailable)
		{
			await context.CallActivityAsync<bool>(nameof(SQLPoolSplitUsageActivity),
				new SplitUsageActivityRequest<SQLDatabaseUsageResponseItem>
				{
					Request = request,
					Data = usageData
				});

		}
		else
		{
			ResourceIdentifier rId = new(request.Resource.ResourceId);

			await context.CallChildWorkflowAsync<bool>(nameof(GroupSubWorkflow),
				new GroupSubWorkflowRequest { ReportId = request.ReportRequest.ReportId, Resources = [request.Resource] },
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(SQLPoolWorkflow)}Group{request.ReportRequest.ReportId}{rId.SubscriptionId}{rId.Name}" });
		}

		return true;
	}
}
