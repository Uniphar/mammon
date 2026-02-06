namespace Mammon.Workflows.Activities;

public class SendReportViaEmail(
	CostCentreReportService costCentreReportService,
	ILogger<SendReportViaEmail> logger) : WorkflowActivity<CostReportRequest, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, CostReportRequest reportRequest)
	{
		ArgumentNullException.ThrowIfNull(reportRequest);

		try
		{
            await costCentreReportService.SendReportToDotFlyerAsync(reportRequest);
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error sending report via email for ReportId: {ReportId}", reportRequest.ReportId);
			throw;
        }

		return true;
	}
}
