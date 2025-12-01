namespace Mammon.Models.CostManagement;

public record CostReportSubscriptionRequest
{    
    public required string SubscriptionId { get; set; }
    public required string SubscriptionName { get; set; }
    public required CostReportRequest ReportRequest { get; set; }
    public GroupingMode GroupingMode { get; set; } = GroupingMode.Resource;
    public string? DevOpsOrganization { get; set; }
}

public enum GroupingMode
{
	Resource,
	Subscription
}

public class CostReportSubscriptionRequestValidator : AbstractValidator<CostReportSubscriptionRequest>
{
    public CostReportSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId must be specified");
        RuleFor(x => x.SubscriptionName).NotEmpty().WithMessage("SubscriptionName must be specified");
        RuleFor(x => x.ReportRequest).NotEmpty().SetValidator(new CostReportRequestValidator());        
    }
}
