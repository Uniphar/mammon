﻿namespace Mammon.Workflows;

public class TenantWorkflow : Workflow<TenantWorkflowRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowContext context, TenantWorkflowRequest input)
	{
		ArgumentNullException.ThrowIfNull(input);

		List<Task<bool>> pendingWorkflows = [];

		foreach (var subscription in input.Subscriptions)
		{
			pendingWorkflows.Add(context.CallChildWorkflowAsync<bool>(nameof(SubscriptionWorkflow),
				new CostReportSubscriptionRequest
				{
					SubscriptionName = subscription,
					ReportRequest = input.ReportRequest
				},
				new ChildWorkflowTaskOptions { InstanceId = $"{nameof(SubscriptionWorkflow)}{subscription}{input.ReportRequest.ReportId}" }));
		}

		await Task.WhenAll(pendingWorkflows);

		await context.CallActivityAsync<bool>(nameof(SendReportViaEmail), input.ReportRequest);

		return true;
	}
}
