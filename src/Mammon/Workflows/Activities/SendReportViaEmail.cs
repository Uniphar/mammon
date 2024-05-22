namespace Mammon.Workflows.Activities;

public class SendReportViaEmail(CostCentreReportService costCentreReportService) : WorkflowActivity<string, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, string reportId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(reportId);

		await costCentreReportService.SendReportToDotFlyerAsync(reportId);

		return true;
	}
}
