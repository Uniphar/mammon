namespace Mammon.Workflows.Activities;

public class SendReportViaEmail(CostCentreReportService costCentreReportService) : WorkflowActivity<CostReportRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, CostReportRequest reportRequest)
	{
		ArgumentNullException.ThrowIfNull(reportRequest);

		await costCentreReportService.SendReportToDotFlyerAsync(reportRequest);

		return true;
	}
}
