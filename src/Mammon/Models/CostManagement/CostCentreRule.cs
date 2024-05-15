namespace Mammon.Models.CostManagement;

/// <summary>
/// this class defines properties and logic of cost centre rule
/// this is what logically maps resources to cost centres
    /// 
/// regular expressions supported for resource name, resource group name, resource type
/// </summary>
public class CostCentreRule
{
    public string[] CostCentres { get; set; } = [];
	public string? ResourceNameMatchPattern { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
	public string? ResourceGroupNameMatchPattern { get; set; }
    public string? SubscriptionId { get; set; }
	public string? ResourceTypeMatchPattern { get; set; }	
    public bool IsDefault { get; set; }
    public bool IsSplittable => CostCentres.Length > 1;

    private Regex? _resourceGroupNameRegExp;
	public Regex? ResourceGroupNameRegExp { 
        get
        {
            if (string.IsNullOrEmpty(ResourceGroupNameMatchPattern))
                return null;
            
            _resourceGroupNameRegExp ??= new Regex(ResourceGroupNameMatchPattern);

            return _resourceGroupNameRegExp;
        }
    }

	private Regex? _resourceNameRegExp;
	public Regex? ResourceNameRegExp
    {
        get
        {
			if (string.IsNullOrEmpty(ResourceNameMatchPattern))
				return null;

			_resourceNameRegExp ??= new Regex(ResourceNameMatchPattern);

			return _resourceNameRegExp;
		}
    }

    public Regex? _resourceTypeRegExp;
	public Regex? ResourceTypeRegExp { 
        get 
        {
			if (string.IsNullOrEmpty(ResourceTypeMatchPattern))
				return null;

			_resourceTypeRegExp ??= new Regex(ResourceTypeMatchPattern);

			return _resourceTypeRegExp;
		} 
    }

	/// <summary>
	/// evaluate match level of available rules to a given resource id and tags
	/// 
	/// this uses the following hierarchy in ascending weight
	///  - tags - 1
	///  - resource type -2
	///  - subscription - 4
	///  - resource group - 8
	///  - resource name - 16
	///  
	/// the more specific match, the higher the score
	/// certain rule definitions are probably only theoretical - e.g. subscription level - but special use cases may apply
	/// (i.e. in the case of resource type and subscription level, subscription rule wins)
	/// 
	/// we support the concept of the default rule
	/// 
	/// </summary>
	/// <param name="resourceId">azure resource id of the target resource</param>
	/// <param name="resourceTags">list of tag names and values associated with the resource instance</param>
	/// <returns>tupple of rule and its evaluated score - 0 - default rule, mismatch - negative, match - positive with higher value indicating more specific match</returns>
	public (int matchScore, CostCentreRule matchedRule) Matches(string resourceId, IDictionary<string, string> resourceTags)
    {
        if (IsDefault) return (0, this);

        ArgumentException.ThrowIfNullOrEmpty(resourceId);

        var parsedResourceId = new ResourceIdentifier(resourceId);

        //if rule for specific field not specified, consider it match
        var resourceNameMatch = ResourceNameRegExp==null || ResourceNameRegExp.IsMatch(parsedResourceId.Name);
        var resourceGroupNameMatch = ResourceGroupNameRegExp==null || (!string.IsNullOrWhiteSpace(parsedResourceId.ResourceGroupName) && ResourceGroupNameRegExp.IsMatch(parsedResourceId.ResourceGroupName));
        var subscriptionIdMatch = string.IsNullOrWhiteSpace(SubscriptionId) || (parsedResourceId.SubscriptionId != null && parsedResourceId.SubscriptionId.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase));
        var resourceTypeMatch = ResourceTypeRegExp==null || ResourceTypeRegExp.IsMatch(parsedResourceId.ResourceType.ToString());
        var tagMatch = MatchTags(resourceTags);

        int scoreMatch = 0;

        //there has to be no mismatch at resource level for score to be affected (e.g. different sub for matching resource name)
        if (resourceNameMatch && resourceGroupNameMatch && subscriptionIdMatch && resourceTypeMatch && tagMatch)
        {
            if (resourceNameMatch && ResourceNameRegExp!=null)
                scoreMatch += 16;
            if (resourceGroupNameMatch && ResourceGroupNameRegExp!=null)
                scoreMatch += 8;
            if (subscriptionIdMatch && !string.IsNullOrWhiteSpace(SubscriptionId))
                scoreMatch += 4;
            if (resourceTypeMatch && ResourceTypeRegExp!=null)
                scoreMatch += 2;
            if (tagMatch && Tags != null && Tags.Count > 0)
                scoreMatch += 1;
        }
        else
            return (-1, this);

        return (scoreMatch, this);
    }
    
    private bool MatchTags(IDictionary<string, string> resourceTags)
    {
        if (Tags == null || Tags.Count == 0)
            return true;

        if (resourceTags == null || resourceTags.Count == 0)
            return false;

        return Tags.All(item => resourceTags.ContainsKey(item.Key) && resourceTags[item.Key] == item.Value);
    }
}

public class CostCentreRuleValidator : AbstractValidator<CostCentreRule>
{
    public CostCentreRuleValidator()
    {
        RuleFor(x => x.CostCentres).NotEmpty();
        RuleFor(x => x).Must(i => i.IsDefault || (!i.IsDefault && (!string.IsNullOrWhiteSpace(i.SubscriptionId) || !string.IsNullOrWhiteSpace(i.ResourceNameMatchPattern) || !string.IsNullOrWhiteSpace(i.ResourceGroupNameMatchPattern)
            || !string.IsNullOrWhiteSpace(i.ResourceTypeMatchPattern) || (i.Tags != null && i.Tags.Count > 0))));

        RuleFor(x => x.ResourceGroupNameMatchPattern).Must((s) => {
            if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); } return true; });

		RuleFor(x => x.ResourceNameMatchPattern).Must((s) => {
			if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); } return true; });

		RuleFor(x => x.ResourceTypeMatchPattern).Must((s) => {
			if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); } return true; });

	}


}
