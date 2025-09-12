namespace Mammon.Models.CostManagement;

public record CostReportSubscriptionRequest
{    
    public required string SubscriptionName { get; init; }
    public required CostReportRequest ReportRequest { get; init; }
    public GroupingMode GroupingMode => GroupingMode.Resource;
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
        RuleFor(x => x.SubscriptionName).NotEmpty().WithMessage("SubscriptionName must be specified");
        RuleFor(x => x.ReportRequest).NotEmpty().SetValidator(new CostReportRequestValidator());        
    }
}
