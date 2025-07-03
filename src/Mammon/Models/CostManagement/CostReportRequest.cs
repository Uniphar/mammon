namespace Mammon.Models.CostManagement;

public record CostReportRequest
{
    public required string ReportId { get; set; }
    public  DateTime CostFrom { get; set; }
    public  DateTime CostTo { get; set; }
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
