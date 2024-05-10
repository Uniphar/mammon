namespace Mammon.Services;

/// <summary>
/// cost centre rule evaluation engine/service
/// 
/// the rule file is expected to be a local file with path configured in
/// </summary>
public class CostCentreRuleEngine
{
    private readonly IConfiguration configuration;
    private IEnumerable<CostCentreRule> CostCentreRules { get; set; } = [];
    public IEnumerable<string> Subscriptions { get; internal set; } = [];
    public IEnumerable<string> CostCentres { get; internal set; } = [];
    public IEnumerable<string> ResourceGroupSuffixRemoveList { get; internal set; } = [];
    public IDictionary<string, string> ResourceGroupTokenClassMap { get; internal set; } = new Dictionary<string, string>();


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

        var definition = JsonSerializer.Deserialize<CostCentreDefinition>(new FileStream(filePath, FileMode.Open, FileAccess.Read), jsonSerializerOptions) 
            ?? throw new InvalidOperationException("Unable to deserialize Cost centre definition");

		new CostCentreDefinitionValidator().ValidateAndThrow(definition);

        CostCentreRules = definition.Rules;
        Subscriptions = definition.Subscriptions;
        ResourceGroupSuffixRemoveList = definition.ResourceGroupSuffixRemoveList;
        CostCentres = CostCentreRules.SelectMany(r => r.CostCentres).Distinct();
        ResourceGroupTokenClassMap = definition.ResourceGroupTokenClassMap ?? new Dictionary<string, string>();
	}

    /// <summary>
    /// find the best matching rule
    /// 
    /// the internal logic is to run through all the rules and highest positive score is returned
    /// </summary>
    /// <param name="resourceId">target resource azure resource id</param>
    /// <param name="tags">target resource tags</param>
    /// <returns>highest matching rule instance</returns>
    public CostCentreRule FindCostCentreRule(string resourceId, IDictionary<string, string> tags)
    {
        var matches = CostCentreRules!
            .Select(x => x.Matches(resourceId, tags))
            .ToList();

        var (_, costCentreRule) = matches
            .Where(x=> x.matchScore>=0)
            .OrderByDescending(x=>x.matchScore)
            .FirstOrDefault(); //default rule is always a fallback

        return costCentreRule;
    }

    public string ProcessResourceGroupName(string resourceId)
    {
        var rgName = new ResourceIdentifier(resourceId).ResourceGroupName ?? "N/A";

		return rgName.RemoveSuffixes(ResourceGroupSuffixRemoveList);
    }

    public string? ClassifyResourceGroup(string rgName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rgName);

        string? ret = null;

		_ = ResourceGroupTokenClassMap.FirstOrDefault(x=>
        {
            if (rgName.Contains(x.Key, StringComparison.OrdinalIgnoreCase))
            {
                ret = x.Value;
                return true;
            }

            return false;
        });

        return ret;
    }
}
