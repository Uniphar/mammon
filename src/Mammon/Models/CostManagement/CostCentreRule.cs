namespace Mammon.Models.CostManagement;

/// <summary>
/// this class defines properties and logic of cost centre rule
/// this is what logically maps resources to cost centres
/// includes support for resource cost splitting - cost split to multiple cost centres based on inner logic and metadata/config
/// </summary>
public class CostCentreRule
{
    public string[] CostCentres { get; set; } = [];
    public string? ResourceName { get; set; }
    public IDictionary<string, string>? Tags { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? SubscriptionId { get; set; }
    public string? ResourceType { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSplittable => CostCentres.Length > 1;

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
    /// <param name="tags">list of tag names and values associated with the resource instance</param>
    /// <returns>tupple of rule and its evaluated score - 0 - default rule, mismatch - negative, match - positive with higher value indicating more specific match</returns>
    public (int matchScore, CostCentreRule matchedRule) Matches(string resourceId, IDictionary<string, string> tags)
    {
        if (IsDefault) return (0, this);

        ArgumentException.ThrowIfNullOrEmpty(resourceId);

        var parsedResourceId = new ResourceIdentifier(resourceId);

        //if rule for specific field not specified, consider it match
        var resourceNameMatch = string.IsNullOrWhiteSpace(ResourceName) || (parsedResourceId.Name.Equals(ResourceName, StringComparison.OrdinalIgnoreCase));
        var resourceGroupNameMatch = string.IsNullOrWhiteSpace(ResourceGroupName) || (parsedResourceId.ResourceGroupName != null && parsedResourceId.ResourceGroupName.Equals(ResourceGroupName, StringComparison.OrdinalIgnoreCase));
        var subscriptionIdMatch = string.IsNullOrWhiteSpace(SubscriptionId) || (parsedResourceId.SubscriptionId != null && parsedResourceId.SubscriptionId.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase));
        var resourceTypeMatch = string.IsNullOrWhiteSpace(ResourceType) || (parsedResourceId.ResourceType.ToString().Equals(ResourceType, StringComparison.OrdinalIgnoreCase));
        var tagMatch = MatchTags(tags);

        int scoreMatch = 0;

        //there has to be no mismatch at resource level for score to be affected (e.g. different sub for matching resource name)
        if (resourceNameMatch && resourceGroupNameMatch && subscriptionIdMatch && resourceTypeMatch && tagMatch)
        {
            if (resourceNameMatch && !string.IsNullOrWhiteSpace(ResourceName))
                scoreMatch += 16;
            if (resourceGroupNameMatch && !string.IsNullOrWhiteSpace(ResourceGroupName))
                scoreMatch += 8;
            if (subscriptionIdMatch && !string.IsNullOrWhiteSpace(SubscriptionId))
                scoreMatch += 4;
            if (resourceTypeMatch && !string.IsNullOrWhiteSpace(ResourceType))
                scoreMatch += 2;
            if (tagMatch && Tags != null && Tags.Count > 0)
                scoreMatch += 1;
        }
        else
            return (-1, this);

        return (scoreMatch, this);
    }

    private bool MatchTags(IDictionary<string, string> tags)
    {
        if (Tags == null || Tags.Count == 0)
            return true;

        if (tags == null || tags.Count == 0)
            return false;

        return Tags.All(item => tags.ContainsKey(item.Key) && tags[item.Key] == item.Value);
    }
}
