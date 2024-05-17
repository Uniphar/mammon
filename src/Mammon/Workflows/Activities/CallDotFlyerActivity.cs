namespace Mammon.Workflows.Activities;

public class CallDotFlyerActivity(CostCentreReportService costCentreReportService) : WorkflowActivity<string, bool>
{
	public override async Task<bool> RunAsync(WorkflowActivityContext context, string input)
	{


		return true;
	}
}
