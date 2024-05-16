namespace Mammon.Models.CostManagement;

public record CostCentreDefinition
{
    public required IList<SubscriptionDefinition> Subscriptions { get; set; }
    public required IList<CostCentreRule> Rules { get; set; }
    public IList<string> ResourceGroupSuffixRemoveList { get; set; } = [];
    public IDictionary<string, string>? ResourceGroupTokenClassMap { get; set; } = new Dictionary<string, string>();
    public IList<string> SpecialModes { get; set; } = [];
}

public class CostCentreDefinitionValidator : AbstractValidator<CostCentreDefinition>
{
    public CostCentreDefinitionValidator()
    {
        RuleFor(x => x.Subscriptions).NotEmpty().WithMessage("Subscription list cannot be empty");
        RuleForEach(x => x.Subscriptions).SetValidator(x=> new SubscriptionDefinitionValidator());
        RuleFor(x => x.Rules).NotEmpty().WithMessage("Rule list cannot be empty");
        RuleForEach(x => x.Rules).SetValidator(x => new CostCentreRuleValidator());
        RuleFor(x => x.Rules).Must(x => x.Where(x => x.IsDefault).Count() == 1).WithMessage("One rule must be set as default");
        RuleForEach(x => x.SpecialModes).NotEmpty().WithMessage("Special mode names must be specified");
    }
}

public class SubscriptionDefinitionValidator : AbstractValidator<SubscriptionDefinition>
{
    public SubscriptionDefinitionValidator()
    {
        RuleFor(x => x.EnvironmentDesignation).NotEmpty().WithMessage("Subscription EnvironmentDesignation must be set");
		RuleFor(x => x.SubscriptionName).NotEmpty().WithMessage("Subscription SubscriptionName must be set");
		RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("Subscription SubscriptionId must be set");
	}
}
