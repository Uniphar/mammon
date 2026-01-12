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
    public IDictionary<string, double>? StaticMySQLMapping { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, double> StaticVisualStudioSubscriptionsMapping { get; set; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    public required decimal VisualStudioEnterpriseMonthlySubscriptionCost { get; set; }
    public required decimal VisualStudioEnterpriseAnnualSubscriptionCost { get; set; }
    public required decimal VisualStudioProfessionalMonthlySubscriptionCost { get; set; }
    public required decimal VisualStudioProfessionalAnnualSubscriptionCost { get; set; }
}

public record DevOpsCostCentreDefinition
{
    public required List<DevOpsCostCentreRule> Rules { get; set; } = new();
    public required string DefaultCostCentre { get; set; } = string.Empty;
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
        RuleFor(x => x.StaticMySQLMapping).SetValidator(x => new StaticMySQLMappingValidator());
        RuleFor(x => x.StaticVisualStudioSubscriptionsMapping).SetValidator(x => new StaticVisualStudioLicensesMapping());
        RuleFor(x => x.VisualStudioEnterpriseMonthlySubscriptionCost).GreaterThanOrEqualTo(0).WithMessage("Visual Studio Enterprise Monthly Subscription Cost must be non-negative");
        RuleFor(x => x.VisualStudioEnterpriseAnnualSubscriptionCost).GreaterThanOrEqualTo(0).WithMessage("Visual Studio Enterprise Annual Subscription Cost must be non-negative");
        RuleFor(x => x.VisualStudioProfessionalMonthlySubscriptionCost).GreaterThanOrEqualTo(0).WithMessage("Visual Studio Professional Monthly Subscription Cost must be non-negative");
        RuleFor(x => x.VisualStudioProfessionalAnnualSubscriptionCost).GreaterThanOrEqualTo(0).WithMessage("Visual Studio Professional Annual Subscription Cost must be non-negative");
    }
}

public class DevOpsCostCentreDefinitionValidator : AbstractValidator<DevOpsCostCentreDefinition>
{
    public DevOpsCostCentreDefinitionValidator()
    {
        RuleFor(x => x.DefaultCostCentre).NotEmpty().WithMessage("Default Cost Centre must be specified");
        RuleForEach(x => x.Rules).SetValidator(x => new DevOpsCostCentreRuleValidator());
    }
}

public class DevOpsCostCentreRuleValidator : AbstractValidator<DevOpsCostCentreRule>
{
    public DevOpsCostCentreRuleValidator()
    {
        RuleFor(t => t.CostCentre).NotEmpty().WithMessage("Cost Centre must be specified");
        RuleFor(t => t.ProjectNameMatchPattern).NotEmpty().WithMessage("Project Name Pattern must be specified");
    }
}

public class StaticMySQLMappingValidator : AbstractValidator<IDictionary<string, double>?>
{
    public StaticMySQLMappingValidator()
    {
        RuleForEach(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).WithMessage("Static MySQL Mapping must have both key and value");
        RuleFor(x => x).Must(HaveUniqueKeys).WithMessage("Static MySQL Mapping must have unique keys");
		RuleFor(x => x).Must(x => x==null || x.Count==0 || x.Values.Sum() == 100d).WithMessage("Split ratios must add up to 100% if specified");
    }

	private static bool HaveUniqueKeys<TKey, TValue>(IDictionary<TKey, TValue>? dictionary)
	{
		return dictionary == null || dictionary.Keys.Distinct().Count() == dictionary.Count;
	}
}

public class StaticVisualStudioLicensesMapping : AbstractValidator<IDictionary<string, double>>
{
    public StaticVisualStudioLicensesMapping()
    {
        RuleForEach(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value >= 0).WithMessage("Static Visual Studio Subscriptions Mapping must have both key and non-negative value");
        RuleFor(x => x).Must(HaveUniqueKeys).WithMessage("Static Visual Studio Subscriptions Mapping must have unique keys");
    }
    private static bool HaveUniqueKeys<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        return dictionary.Keys.Distinct().Count() == dictionary.Count;
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
