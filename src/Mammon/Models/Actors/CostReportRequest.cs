namespace Mammon.Models.Actors;

public sealed class CostReportRequest
{
    public string ReportId { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public DateTime CostFrom { get; set; }
    public DateTime CostTo { get; set; }
}

public class CostReportRequestValidator : AbstractValidator<CostReportRequest>
{
    public CostReportRequestValidator()
    {
        RuleFor(x => x.SubscriptionName).NotEmpty();
        RuleFor(x => x.CostFrom).NotEmpty();
        RuleFor(x => x.CostFrom).NotEmpty();
        RuleFor(x => x.CostTo).GreaterThan(x => x.CostFrom);
    }
}
