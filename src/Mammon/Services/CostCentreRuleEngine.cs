namespace Mammon.Services;

/// <summary>
/// cost centre rule evaluation engine/service
/// 
/// the rule file - will be replaced by database - is expected to be a local file with path configured in
/// </summary>
public class CostCentreRuleEngine
{
    private readonly IConfiguration configuration;
    private IEnumerable<CostCentreRule> CostCentreRules { get; set; } = [];
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };


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

        var definition = JsonSerializer.Deserialize<CostCentreDefinition>(new FileStream(filePath, FileMode.Open, FileAccess.Read), jsonSerializerOptions);
        
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
