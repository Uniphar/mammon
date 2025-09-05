namespace Mammon.Models.CostManagement;

public record CostReportRequest
{
    public required string ReportId { get; init; }
    public DateTime CostFrom { get; init; }
    public DateTime CostTo { get; init; }
}

public class CostReportRequestValidator : AbstractValidator<CostReportRequest>
{
    public CostReportRequestValidator()
    {
        RuleFor(x => x.CostFrom).NotEmpty().WithMessage("CostFrom must be set");
        RuleFor(x => x.ReportId).NotEmpty().WithMessage("ReportId must be set");
        RuleFor(x => x.CostTo).GreaterThan(x => x.CostFrom).WithMessage("CostTo must be later datetime than CostFrom");
    }
}
