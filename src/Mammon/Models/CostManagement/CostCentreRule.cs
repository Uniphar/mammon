﻿namespace Mammon.Models.CostManagement;

/// <summary>
/// this class defines properties and logic of cost centre rule
/// this is what logically maps resources to cost centres
/// 
/// regular expressions supported for resource name, resource group name, resource type
/// </summary>
public class CostCentreRule
{
	public string CostCentre { get; set; } = string.Empty;
	public string? ResourceNameMatchPattern { get; set; }
	public IDictionary<string, string>? Tags { get; set; }
	public string? ResourceGroupNameMatchPattern { get; set; }
	public string? SubscriptionId { get; set; }
	public string? ResourceTypeMatchPattern { get; set; }
	public string? FullIdMatchPattern { get; set; }
	public bool IsDefault { get; internal set; }

	private Regex? _resourceGroupNameRegExp;
	public Regex? ResourceGroupNameRegExp => GetRegExp(ResourceGroupNameMatchPattern, ref _resourceGroupNameRegExp);

	private Regex? _resourceNameRegExp;
	public Regex? ResourceNameRegExp => GetRegExp(ResourceNameMatchPattern, ref _resourceNameRegExp);

	public Regex? _resourceTypeRegExp;
	public Regex? ResourceTypeRegExp => GetRegExp(ResourceTypeMatchPattern, ref _resourceTypeRegExp);

	public Regex? FullIdRegExp => GetRegExp(FullIdMatchPattern, ref _fullIdRegExp);
	private Regex? _fullIdRegExp;

	/// <summary>
	/// evaluate match level of available rules to a given resource id and tags
	/// 
	/// this uses the following hierarchy in ascending weight
	///  - tags - 1
	///  - resource type -2
	///  - subscription - 4
	///  - resource group - 8
	///  - resource name - 16
	///  - full id - 32
	///  
	/// the more specific match, the higher the score
	/// (i.e. in the case of resource type level rule or subscription level rule, subscription rule wins)
	/// 
	/// certain rule definitions are probably only theoretical - e.g. subscription level - but special use cases may apply
	/// 
	/// we support the concept of the default rule
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
		var resourceNameMatch = ResourceNameRegExp == null || ResourceNameRegExp.IsMatch(parsedResourceId.Name);
		var resourceGroupNameMatch = ResourceGroupNameRegExp == null || (!string.IsNullOrWhiteSpace(parsedResourceId.ResourceGroupName) && ResourceGroupNameRegExp.IsMatch(parsedResourceId.ResourceGroupName));
		var subscriptionIdMatch = string.IsNullOrWhiteSpace(SubscriptionId) || (parsedResourceId.SubscriptionId != null && parsedResourceId.SubscriptionId.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase));
		var resourceTypeMatch = ResourceTypeRegExp == null || ResourceTypeRegExp.IsMatch(parsedResourceId.ResourceType.ToString());
		var tagMatch = MatchTags(resourceTags);
		var fullIdMatch = FullIdRegExp==null || FullIdRegExp.IsMatch(resourceId);

		int scoreMatch = 0;

		//there has to be no mismatch at resource level for score to be affected (e.g. different sub for matching resource name)
		if (resourceNameMatch && resourceGroupNameMatch && subscriptionIdMatch && resourceTypeMatch && tagMatch && fullIdMatch)
		{
			if (fullIdMatch && FullIdRegExp!=null)
				scoreMatch += 32;
			if (resourceNameMatch && ResourceNameRegExp != null)
				scoreMatch += 16;
			if (resourceGroupNameMatch && ResourceGroupNameRegExp != null)
				scoreMatch += 8;
			if (subscriptionIdMatch && !string.IsNullOrWhiteSpace(SubscriptionId))
				scoreMatch += 4;
			if (resourceTypeMatch && ResourceTypeRegExp != null)
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

	private static Regex? GetRegExp(string? input, ref Regex? regex)
	{
		if (string.IsNullOrEmpty(input))
			return null;

		return regex ??= new Regex(input);
	}
}

public class CostCentreRuleValidator : AbstractValidator<CostCentreRule>
{
	public CostCentreRuleValidator()
	{
		RuleFor(x => x.CostCentre).NotEmpty().WithMessage("Cost Centre must be assigned");
		RuleFor(x => x).Must(i => !i.IsDefault && (!string.IsNullOrWhiteSpace(i.SubscriptionId) || !string.IsNullOrWhiteSpace(i.ResourceNameMatchPattern) || !string.IsNullOrWhiteSpace(i.ResourceGroupNameMatchPattern)
			|| !string.IsNullOrWhiteSpace(i.ResourceTypeMatchPattern) || (i.Tags != null && i.Tags.Count > 0) || !string.IsNullOrWhiteSpace(i.FullIdMatchPattern)))
			.WithMessage("At least one differentiator must be set");

		RuleFor(x => x.ResourceGroupNameMatchPattern).Must((s) =>
		{
			if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); }
			return true;
		})
			.WithMessage("ResourceGroupNameMatchPattern must be a string or regexp expression");

		RuleFor(x => x.ResourceNameMatchPattern).Must((s) =>
		{
			if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); }
			return true;
		})
			.WithMessage("ResourceNameMatchPattern must be a string or regexp expression");

		RuleFor(x => x.ResourceTypeMatchPattern).Must((s) =>
		{
			if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); }
			return true;
		})
			.WithMessage("ResourceTypeMatchPattern must be a string or regexp expression");

		RuleFor(x => x.FullIdMatchPattern).Must((s) =>
		{
			if (!string.IsNullOrWhiteSpace(s)) { _ = new Regex(s); }
			return true;
		})
			.WithMessage("FullIdMatchPattern must be a string or regexp expression");

	}
}
