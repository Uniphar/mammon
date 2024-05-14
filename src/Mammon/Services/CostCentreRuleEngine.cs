﻿using System.Security.Cryptography;

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
    public IList<SubscriptionDefinition> Subscriptions { get; internal set; } = [];
    public IEnumerable<string> CostCentres { get; internal set; } = [];
    public IEnumerable<string> ResourceGroupSuffixRemoveList { get; internal set; } = [];
    public IDictionary<string, string> ResourceGroupTokenClassMap { get; internal set; } = new Dictionary<string, string>();
    public IEnumerable<string> SubscriptionNames { get; internal set; } = [];
	public IList<string> SpecialModes { get; set; } = [];

	private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

    private const string DevBoxSpecialMode = "DevBoxPoolNameGrouping";

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
		SubscriptionNames = Subscriptions.Select(x => x.SubscriptionName);
        SpecialModes = definition.SpecialModes;
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

    public CostReportPivotEntry ProjectCostReportPivotEntry(string resourceId, double cost)
    {
        var rId = new ResourceIdentifier(resourceId);

		var rgName = rId.ResourceGroupName ?? rId.ResourceType;
        var subId = rId.SubscriptionId ?? "N/A";

        if (IsModeEnabled(DevBoxSpecialMode) && rId.IsDevBoxPool())
        {
            return new CostReportPivotEntry() { PivotName = rId.GetDevBoxPoolName(), ResourceId = resourceId, SubscriptionId = subId, Cost = cost };
        }
        else
		    return new CostReportPivotEntry() { PivotName = rgName.RemoveSuffixes(ResourceGroupSuffixRemoveList), ResourceId = resourceId, SubscriptionId = subId, Cost = cost };
    }   

    private bool IsModeEnabled(string modeName)
    {
        return SpecialModes.Contains(modeName, StringComparer.OrdinalIgnoreCase); 
    }
    public string? ClassifyPivot(CostReportPivotEntry pivotDefinition)
    {
        ArgumentNullException.ThrowIfNull(pivotDefinition);

        ResourceIdentifier rId = new(pivotDefinition.ResourceId);
        if (IsModeEnabled(DevBoxSpecialMode) && rId.IsDevBoxPool())
        {
            return "DevBox Pool";
        }
        else
        {
            string? ret = null;

            _ = ResourceGroupTokenClassMap.FirstOrDefault(x =>
            {
                if (pivotDefinition.PivotName.Contains(x.Key, StringComparison.OrdinalIgnoreCase))
                {
                    ret = x.Value;
                    return true;
                }

                return false;
            });

            return ret;
        }
    }

    public string LookupEnvironment(string  subId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subId);

        return Subscriptions.FirstOrDefault(x => x.SubscriptionId == subId)?.EnvironmentDesignation ?? "unspecified";
    }
}

public record CostReportPivotEntry
{   
    public required string PivotName { get; set; } 
    public required string ResourceId { get; set; }
    public required string SubscriptionId { get; set; }
    public required double Cost { get; set; }
}
