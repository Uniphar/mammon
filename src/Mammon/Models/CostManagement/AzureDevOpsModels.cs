using Newtonsoft.Json;

namespace Mammon.Models.CostManagement;

public class MemberEntitlementItem
{
    [JsonProperty("member")]
    public Member Member { get; set; } = new();

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("accessLevel")]
    public AccessLevel AccessLevel { get; set; } = new();

    [JsonProperty("lastAccessedDate")]
    public DateTime? LastAccessedDate { get; set; }

    [JsonProperty("dateCreated")]
    public DateTime? DateCreated { get; set; }

    [JsonProperty("projectEntitlements")]
    public List<ProjectEntitlement> ProjectEntitlements { get; set; } = new();
}

public class Member
{
    [JsonProperty("subjectKind")]
    public string SubjectKind { get; set; } = string.Empty;

    [JsonProperty("metaType")]
    public string MetaType { get; set; } = string.Empty;

    [JsonProperty("directoryAlias")]
    public string DirectoryAlias { get; set; } = string.Empty;

    [JsonProperty("domain")]
    public string Domain { get; set; } = string.Empty;

    [JsonProperty("principalName")]
    public string PrincipalName { get; set; } = string.Empty;

    [JsonProperty("mailAddress")]
    public string MailAddress { get; set; } = string.Empty;

    [JsonProperty("origin")]
    public string Origin { get; set; } = string.Empty;

    [JsonProperty("originId")]
    public string OriginId { get; set; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("descriptor")]
    public string Descriptor { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

public class AccessLevel
{
    [JsonProperty("licensingSource")]
    public string LicensingSource { get; set; } = string.Empty;

    [JsonProperty("accountLicenseType")]
    public string AccountLicenseType { get; set; } = string.Empty;

    [JsonProperty("msdnLicenseType")]
    public string MsdnLicenseType { get; set; } = string.Empty;

    [JsonProperty("gitHubLicenseType")]
    public string GitHubLicenseType { get; set; } = string.Empty;

    [JsonProperty("licenseDisplayName")]
    public string LicenseDisplayName { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("statusMessage")]
    public string StatusMessage { get; set; } = string.Empty;

    [JsonProperty("assignmentSource")]
    public string AssignmentSource { get; set; } = string.Empty;
}

public class ProjectEntitlement
{
    [JsonProperty("group")]
    public Group Group { get; set; } = new();

    [JsonProperty("projectRef")]
    public ProjectRef ProjectRef { get; set; } = new();
}

public class Group
{
    [JsonProperty("groupType")]
    public string GroupType { get; set; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("descriptor")]
    public string Descriptor { get; set; } = string.Empty;
}

public class ProjectRef
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

public record DevOpsProjectsCosts
{
    public required ResourceCost? UnassignedCost { get; set; }
    public required List<DevOpsProjectCost> ProjectCosts { get; set; } = new();

    public bool IsEmpty() => UnassignedCost == null && ProjectCosts.Count == 0;
}


public record DevOpsProjectCost
{
    public required string ProjectName { get; set; } = string.Empty;
    public required Dictionary<string, ResourceCost> ContributingGroupCosts { get; set; } = new();
}