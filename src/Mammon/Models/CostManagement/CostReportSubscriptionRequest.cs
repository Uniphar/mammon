namespace Mammon.Models.CostManagement;

public record CostReportSubscriptionRequest
{
    public required string ReportId { get; set; }
    public required string SubscriptionName { get; set; }
    public required DateTime CostFrom { get; set; }
    public required DateTime CostTo { get; set; }
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
