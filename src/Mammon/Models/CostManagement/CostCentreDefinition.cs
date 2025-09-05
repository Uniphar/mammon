namespace Mammon.Models.CostManagement;

public record CostCentreDefinition
{
    public required IList<SubscriptionDefinition> Subscriptions { get; init; }
    public IList<CostCentreRule> Rules { get; init; } = [];
    public required string DefaultCostCentre { get; init; }
    public IList<string> ResourceGroupSuffixRemoveList { get; init; } = [];
    public IDictionary<string, string>? ResourceGroupTokenClassMap { get; init; } = new Dictionary<string, string>();
    public IList<SpecialModeDefinition> SpecialModes { get; init; } = [];
    public IDictionary<string, string> AKSNamespaceMapping { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> SQLDatabaseMapping { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> GroupIDMapping { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, double>? StaticMySQLMapping { get; init; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
}

public class CostCentreDefinitionValidator : AbstractValidator<CostCentreDefinition>
{
    public CostCentreDefinitionValidator()
    {
        RuleFor(x => x.Subscriptions).NotEmpty().WithMessage("Subscription list cannot be empty");
        RuleForEach(x => x.Subscriptions).SetValidator(x => new SubscriptionDefinitionValidator());
        RuleFor(x => x.DefaultCostCentre).NotEmpty().WithMessage("Default Cost Centre must be specified");
        RuleForEach(x => x.Rules).SetValidator(x => new CostCentreRuleValidator());
        RuleFor(x => x.Rules).Must(x => !x.Any(x => x.IsDefault)).WithMessage("Default rule is implicit");
        RuleForEach(x => x.SpecialModes).NotEmpty().WithMessage("Special mode names must be specified");
        RuleForEach(x => x.AKSNamespaceMapping).Must(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .WithMessage("AKS Namespace Mapping must have both key and value");
        RuleForEach(x => x.SQLDatabaseMapping).Must(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .WithMessage("SQL Database Mapping must have both key and value");
        RuleForEach(x => x.GroupIDMapping).Must(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .WithMessage("Group ID Mapping must have both key and value");
        RuleFor(x => x.StaticMySQLMapping).SetValidator(x => new StaticMySQLMappingValidator());
    }
}

public class StaticMySQLMappingValidator : AbstractValidator<IDictionary<string, double>?>
{
    public StaticMySQLMappingValidator()
    {
        RuleForEach(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).WithMessage("Static MySQL Mapping must have both key and value");
        RuleFor(x => x).Must(HaveUniqueKeys).WithMessage("Static MySQL Mapping must have unique keys");
        RuleFor(x => x).Must(x => x == null || x.Count == 0 || x.Values.Sum() == 100d).WithMessage("Split ratios must add up to 100% if specified");
    }

    private static bool HaveUniqueKeys<TKey, TValue>(IDictionary<TKey, TValue>? dictionary)
    {
        return dictionary == null || dictionary.Keys.Distinct().Count() == dictionary.Count;
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