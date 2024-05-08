namespace Mammon.Models.CostManagement;

public record CostReportRequest
{
    public required string ReportId { get; set; }
    public required DateTime CostFrom { get; set; }
    public required DateTime CostTo { get; set; }
}

public class CostReportRequestValidator : AbstractValidator<CostReportRequest>
{
    public CostReportRequestValidator()
    {
        RuleFor(x => x.CostFrom).NotEmpty();
        RuleFor(x => x.CostFrom).NotEmpty();
        RuleFor(x => x.CostTo).GreaterThan(x => x.CostFrom);
    }
}
