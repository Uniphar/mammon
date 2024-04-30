namespace Mammon.Services;

public class CostCentreRuleEngine
{
    private readonly IConfiguration configuration;
    private IEnumerable<CostCentreRule> CostCentreRules { get; set; } = [];

    public CostCentreRuleEngine(IConfiguration configuration)
    {
        this.configuration = configuration;    
        Initialize();
    }

    private void Initialize()
    {
        var filePath = configuration[Consts.CostCentreRuleEngineFilePathConfigKey];

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new InvalidOperationException($"Unable to locate file: {filePath}");

        var definition = JsonSerializer.Deserialize<CostCentreDefinition>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        
        if (definition == null || definition.Rules==null || definition.Rules.Count==0)
            throw new InvalidOperationException("Unable to load cost centre rules definitions");

        CostCentreRules = definition.Rules;
        Validate();
    }

    private void Validate()
    {
        if (CostCentreRules == null || CostCentreRules.Count() == 0)
            throw new InvalidOperationException("Invalid rule specification");

        //single default must exist
        var defaults = CostCentreRules.Where(x => x.IsDefault);

        if (defaults != null && defaults.Count() != 1)
            throw new InvalidOperationException("Single default rule is expected");

        if (CostCentreRules.Any(x => !x.IsDefault && string.IsNullOrWhiteSpace(x.SubscriptionId) && string.IsNullOrWhiteSpace(x.ResourceName) && string.IsNullOrWhiteSpace(x.ResourceGroupName)
            && string.IsNullOrWhiteSpace(x.ResourceType) && (x.Tags==null || x.Tags.Count==0) ))
            throw new InvalidOperationException("No empty rule may be present");
    }

    /// <summary>
    /// find the best matchong 
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public CostCentreRule FindCostCentreRule(string resourceId, IDictionary<string, string> tags)
    {
        var matches = CostCentreRules!
            .Select(x => x.Matches(resourceId, tags))
            .ToList();

        var (_, costCentreRule) = matches
            .Where(x=> x.matchScore>=0)
            .OrderByDescending(x=>x.matchScore)
            .FirstOrDefault();

        return costCentreRule;
    }
}
