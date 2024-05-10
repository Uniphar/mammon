namespace Mammon.Models.CostManagement;

public record CostCentreDefinition
{
    public required IList<string> Subscriptions { get; set; }
    public required IList<CostCentreRule> Rules { get; set; }
    public IList<string> ResourceGroupSuffixRemoveList { get; set; } = [];
    public IDictionary<string, string>? ResourceGroupTokenClassMap { get; set; } = new Dictionary<string, string>();
}

public class CostCentreDefinitionValidator : AbstractValidator<CostCentreDefinition>
{
    public CostCentreDefinitionValidator()
    {
        RuleFor(x =>x.Subscriptions).NotEmpty();
        RuleFor(x => x.Rules).NotEmpty();
        RuleForEach(x => x.Rules).SetValidator(x => new CostCentreRuleValidator());
        RuleFor(x => x.Rules).Must(x => x.Where(x => x.IsDefault).Count() == 1);
    }
}
