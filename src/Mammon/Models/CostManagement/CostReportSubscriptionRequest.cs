namespace Mammon.Models.CostManagement;

public sealed class CostReportSubscriptionRequest
{
    public string ReportId { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public DateTime CostFrom { get; set; }
    public DateTime CostTo { get; set; }
}

public class CostReportSubscriptionRequestValidator : AbstractValidator<CostReportSubscriptionRequest>
{
    public CostReportSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionName).NotEmpty();
        RuleFor(x => x.CostFrom).NotEmpty();
        RuleFor(x => x.CostFrom).NotEmpty();
        RuleFor(x => x.CostTo).GreaterThan(x => x.CostFrom);
    }
}
