namespace Mammon.Models.Workflows.Activities;

public record ObtainCostsActivityRequest
{
	public required string SubscriptionName { get; init; }
	public required GroupingMode GroupingMode { get; init; } = GroupingMode.Resource;
	public required DateTime CostFrom { get; init; }
	public required DateTime CostTo { get; init;}
	public required int PageIndex { get; init; }	 // 0-based
}

public class ObtainCostsActivityRequestValidator : AbstractValidator<ObtainCostsActivityRequest>
{
	public ObtainCostsActivityRequestValidator()
	{
		RuleFor(t => t.SubscriptionName).NotEmpty().WithMessage("SubscriptionName must be specified");
	}
}
