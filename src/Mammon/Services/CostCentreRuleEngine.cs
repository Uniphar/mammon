namespace Mammon.Services;

/// <summary>
/// cost centre rule evaluation engine/service
/// 
/// the rule file is expected to be a local file with path configured in
/// </summary>
public class CostCentreRuleEngine
{
	private readonly IConfiguration configuration;
	private IList<CostCentreRule> CostCentreRules { get; set; } = [];
	public IList<SubscriptionDefinition> Subscriptions { get; internal set; } = [];
	public IEnumerable<string> CostCentres { get; internal set; } = [];
	public string DefaultCostCentre { get; set; } = string.Empty;
	public IEnumerable<string> ResourceGroupSuffixRemoveList { get; internal set; } = [];
	public IDictionary<string, string> ResourceGroupTokenClassMap { get; internal set; } = new Dictionary<string, string>();
	public IEnumerable<string> SubscriptionNames { get; internal set; } = [];
	public IList<SpecialModeDefinition> SpecialModes { get; set; } = [];
	public IDictionary<string, string> AKSNamespaceMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public IDictionary<string, string> GroupIDMapping { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public IDictionary<Regex, string> SQLDatabaseMapping { get; set; } = new Dictionary<Regex, string>();

	private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

	private const string DevBoxSpecialMode = "DevBoxPoolNameGrouping";

	private const string RGRegexpFormat = ".+\\/resourceGroups\\/{0}\\/.+";

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

		AKSNamespaceMapping = definition.AKSNamespaceMapping;
		CostCentreRules = definition.Rules;
		DefaultCostCentre = definition.DefaultCostCentre;
		CostCentreRules.Add(new CostCentreRule { CostCentre = DefaultCostCentre, IsDefault = true });
		Subscriptions = definition.Subscriptions;
		ResourceGroupSuffixRemoveList = definition.ResourceGroupSuffixRemoveList;		
		SubscriptionNames = Subscriptions.Select(x => x.SubscriptionName);
		SpecialModes = definition.SpecialModes;
		SQLDatabaseMapping = definition.SQLDatabaseMapping.ToDictionary(x => new Regex(x.Key), x=> x.Value);
		GroupIDMapping = definition.GroupIDMapping;
		ResourceGroupTokenClassMap = definition.ResourceGroupTokenClassMap ?? new Dictionary<string, string>();

		InitializeCostCentres();
	}

	private void InitializeCostCentres()
	{
		var list = CostCentreRules.Select(r => r.CostCentre).Distinct().ToList();
		list.AddRange(AKSNamespaceMapping.Values.Distinct());
		list.AddRange(SQLDatabaseMapping.Values.Distinct());

		CostCentres = list.Distinct();
	}

	public string FindCostCentre(string resourceId, IDictionary<string, string> tags)
	{
		return FindCostCentreRule(resourceId, tags).CostCentre;
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
			.Where(x => x.matchScore >= 0)
			.OrderByDescending(x => x.matchScore)
			.FirstOrDefault(); //default rule is always a fallback

		return costCentreRule;
	}

	public string GetCostCentreForAKSNamespace(string aksNS)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aksNS);

		return AKSNamespaceMapping.TryGetValue(aksNS, out string? value) ? value : DefaultCostCentre;
	}

	public string GetCostCentreForSQLDatabase(string dbName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dbName);
		KeyValuePair<Regex, string> match;

		if (!(match = SQLDatabaseMapping.FirstOrDefault(x => x.Key.IsMatch(dbName))).Equals(default(KeyValuePair<Regex, string>)))
			return match.Value;

		return DefaultCostCentre;
	}

	public string GetCostCentreForGroupID(string groupId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

		return GroupIDMapping.TryGetValue(groupId, out string? value) ? value : DefaultCostCentre;
	}

	public CostReportPivotEntry ProjectCostReportPivotEntry(string resourceId, ResourceCost cost)
	{
		var rId = new ResourceIdentifier(resourceId);

		var rgName = rId.ResourceGroupName ?? rId.ResourceType;
		var subId = rId.SubscriptionId ?? "N/A";

		string pivotName;

		if (IsModeEnabled(DevBoxSpecialMode) && rId.IsDevBoxPool())
			pivotName = rId.GetDevBoxProjectName();
		else
			pivotName = rgName.RemoveSuffixes(ResourceGroupSuffixRemoveList);

		return new CostReportPivotEntry() { PivotName = pivotName, ResourceId = resourceId, SubscriptionId = subId, Cost = cost };
	}

	public bool IsModeEnabled(string modeName)
	{
		return SpecialModes.Any(x=>x.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));
	}

	public void ProjectModes(ResourceCostResponse resource)
	{
		foreach (var mode in SpecialModes)
		{
			if (IsModeEnabled(mode.Name, resource.ResourceIdentifier))
				resource.EnabledModes.Add(mode.Name);
		}
	}

	public bool IsModeEnabled(string modeName, ResourceIdentifier resourceIdentifier)
	{
		var mode = SpecialModes.FirstOrDefault(x => x.Name.Equals(modeName, StringComparison.OrdinalIgnoreCase));
		if (mode == null) return false;
		
		if (!mode.ResourceGroupFilter.Any())
			return true;
		else
		{
			return mode.ResourceGroupFilter.Any(x => 
			{
				Regex r = new(string.Format(RGRegexpFormat, x));
				return r.IsMatch(resourceIdentifier.ToString());
			});
		}	
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

	public string LookupEnvironment(string subId)
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
	public required ResourceCost Cost { get; set; }
}
