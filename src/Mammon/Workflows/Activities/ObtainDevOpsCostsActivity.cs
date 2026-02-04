namespace Mammon.Workflows.Activities;

public class ObtainDevOpsCostsActivity(
    CostRetrievalService costService,
    ILogger<ObtainDevOpsCostsActivity> logger) : WorkflowActivity<ObtainDevOpsCostsActivityRequest, ObtainLicensesCostWorkflowResult>
{
    public const string BasicLicenseProductName = "Azure Repos and Boards (Basic)";
    public const string BasicPlusTestPlansLicenseProductName = "Azure Test Plans - Standard";

    public override async Task<ObtainLicensesCostWorkflowResult> RunAsync(WorkflowActivityContext context, ObtainDevOpsCostsActivityRequest request)
    {
        try
        {
            var devOpsCost = await costService.QueryDevOpsCostForSubAsync(request);

            decimal basicLicensesCost = devOpsCost.SingleOrDefault(t => t.Product == BasicLicenseProductName)?.Cost.Cost ?? 0m;
            string basicLicenseCurrency = devOpsCost.SingleOrDefault(t => t.Product == BasicLicenseProductName)?.Cost.Currency ?? "EUR";

            decimal testPlansLicensesCost = devOpsCost.SingleOrDefault(t => t.Product == BasicPlusTestPlansLicenseProductName)?.Cost.Cost ?? 0m;
            string testPlansLicenseCurrency = devOpsCost.SingleOrDefault(t => t.Product == BasicPlusTestPlansLicenseProductName)?.Cost.Currency ?? "EUR";

            return new ObtainLicensesCostWorkflowResult
            {
                TotalBasicLicensesCost = new ResourceCost(basicLicensesCost, basicLicenseCurrency),
                TotalBasicPlusTestPlansLicensesCost = new ResourceCost(testPlansLicensesCost, testPlansLicenseCurrency)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obtaining DevOps licenses cost for ReportId: {ReportId}", request.ReportId);
            throw;
        }
    }
}
