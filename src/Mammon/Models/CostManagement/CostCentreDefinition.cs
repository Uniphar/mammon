namespace Mammon.Models.CostManagement;

public record CostCentreDefinition
{
    public required IList<SubscriptionDefinition> Subscriptions { get; set; }
    public IList<CostCentreRule> Rules { get; set; } = [];
    public required string DefaultCostCentre { get; set; }
    public IList<string> ResourceGroupSuffixRemoveList { get; set; } = [];
    public IDictionary<string, string>? ResourceGroupTokenClassMap { get; set; } = new Dictionary<string, string>();	
	public IList<SpecialModeDefinition> SpecialModes { get; set; } = [];
	public IDictionary<string, string> AKSNamespaceMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public IDictionary<string, string> SQLDatabaseMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public IDictionary<string, string> GroupIDMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class CostCentreDefinitionValidator : AbstractValidator<CostCentreDefinition>
{
    public CostCentreDefinitionValidator()
    {
        RuleFor(x => x.Subscriptions).NotEmpty().WithMessage("Subscription list cannot be empty");
        RuleForEach(x => x.Subscriptions).SetValidator(x=> new SubscriptionDefinitionValidator());
        RuleFor(x => x.DefaultCostCentre).NotEmpty().WithMessage("Default Cost Centre must be specified");
        RuleForEach(x => x.Rules).SetValidator(x => new CostCentreRuleValidator());
        RuleFor(x => x.Rules).Must(x => !x.Any(x => x.IsDefault)).WithMessage("Default rule is implicit");
        RuleForEach(x => x.SpecialModes).NotEmpty().WithMessage("Special mode names must be specified");
        RuleForEach(x => x.AKSNamespaceMapping).Must(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value)).WithMessage("AKS Namespace Mapping must have both key and value");
		RuleForEach(x => x.SQLDatabaseMapping).Must(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value)).WithMessage("SQL Database Mapping must have both key and value");
		RuleForEach(x => x.GroupIDMapping).Must(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value)).WithMessage("Group ID Mapping must have both key and value");
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
