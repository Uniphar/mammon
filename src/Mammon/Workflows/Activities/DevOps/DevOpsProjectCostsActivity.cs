namespace Mammon.Workflows.Activities.DevOps;

public class DevOpsProjectCostsActivity : WorkflowActivity<DevOpsMapProjectCostsActivityRequest, DevOpsProjectsCosts>
{
    private const string BasicLicenseName = "Basic";
    private const string BasicPlusTestPlansLicenseName = "Basic + Test Plans";

    public static readonly HashSet<string> SkipGroups =
    [
        "Build Administrators",
        "Contributors",
        "Endpoint Administrators",
        "Endpoint Creators",
        "Project Administrators",
        "Project Valid Users",
        "Readers",
        "Release Administrators",
        "Project Readers"
    ];

    public override async Task<DevOpsProjectsCosts> RunAsync(WorkflowActivityContext context, DevOpsMapProjectCostsActivityRequest input)
    {
        var currency = input.LicenseCosts.TotalBasicLicensesCost.Currency;

        // Separate users by license type and calculate price per user
        var basicUsers = input.MemberEntitlements.Where(m => m.AccessLevel.LicenseDisplayName == BasicLicenseName).ToList();
        var testPlansUsers = input.MemberEntitlements.Where(m => m.AccessLevel.LicenseDisplayName == BasicPlusTestPlansLicenseName).ToList();

        var basicPricePerUser = basicUsers.Count > 0 ? input.LicenseCosts.TotalBasicLicensesCost.Cost / basicUsers.Count : 0m;
        var testPlansPricePerUser = testPlansUsers.Count > 0 ? input.LicenseCosts.TotalBasicPlusTestPlansLicensesCost.Cost / testPlansUsers.Count : 0m;


        // Map users to projects and groups
        // UserId -> [ ProjectName -> Set of Groups ]
        var userProjects = CreateUsersToProjectsAndGroupsMap(input.MemberEntitlements);

        var projectsToGroupCosts = new Dictionary<string, DevOpsProjectCost>(StringComparer.OrdinalIgnoreCase);
        (projectsToGroupCosts, var unassignedBasicCost) = DistributeSharesAcrossActiveGroups(userProjects, projectsToGroupCosts, basicUsers, basicPricePerUser, currency);
        (projectsToGroupCosts, var unassignedTestCost) = DistributeSharesAcrossActiveGroups(userProjects, projectsToGroupCosts, testPlansUsers, testPlansPricePerUser, currency);

        return new DevOpsProjectsCosts
        {
            ProjectCosts = projectsToGroupCosts.Values.ToList(),
            UnassignedCost = unassignedBasicCost != null && unassignedTestCost != null
                ? new ResourceCost([new(unassignedBasicCost.Cost, currency), new(unassignedTestCost.Cost, currency)])
                : unassignedBasicCost ?? unassignedTestCost
        };
    }

    private static Dictionary<string, Dictionary<string, HashSet<string>>> CreateUsersToProjectsAndGroupsMap(
        List<MemberEntitlementItem> members)
    {
        var userProjects = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in members)
        {
            var memberId = member.Id;

            foreach (var projectEntitlement in member.ProjectEntitlements ?? Enumerable.Empty<ProjectEntitlement>())
            {
                // Skip groups in which the User doesn't have an Active role.
                if (SkipGroups.Contains(projectEntitlement.Group.DisplayName))
                    continue;

                var projectName = projectEntitlement.ProjectRef.Name;
                if (string.IsNullOrWhiteSpace(projectName))
                    continue;

                if (!userProjects.TryGetValue(memberId, out var projectToGroups))
                {
                    projectToGroups = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    userProjects[memberId] = projectToGroups;
                }

                if (!projectToGroups.TryGetValue(projectName, out var groups))
                {
                    groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    projectToGroups[projectName] = groups;
                }

                groups.Add(projectEntitlement.Group.DisplayName);
            }
        }

        return userProjects;
    }

    private static (Dictionary<string, DevOpsProjectCost>, ResourceCost?) DistributeSharesAcrossActiveGroups(
        Dictionary<string, Dictionary<string, HashSet<string>>> userProjects,
        Dictionary<string, DevOpsProjectCost> projectCost,
        List<MemberEntitlementItem> members,
        decimal licensePricePerUser,
        string currency)
    {
        var unassignedUsersCount = 0;
        foreach (var userId in members.Select(member => member.Id))
        {
            // Obtain projects and groups for the user
            if (!userProjects.TryGetValue(userId, out var projectsToGroups) || projectsToGroups.Count == 0)
            {
                unassignedUsersCount++;
                continue;
            }

            // Calculate share per project
            var share = licensePricePerUser / projectsToGroups.Count;

            // Foreach project and group, assign cost
            foreach (var projectToGroup in projectsToGroups)
            {
                var project = projectToGroup.Key;
                var groups = projectToGroup.Value;

                if (!projectCost.TryGetValue(project, out var cost))
                {
                    cost = new DevOpsProjectCost
                    {
                        ProjectName = project,
                        ContributingGroupCosts = new Dictionary<string, ResourceCost>(StringComparer.OrdinalIgnoreCase)
                    };
                    projectCost[project] = cost;
                }

                // Either add to existing group cost or create new
                foreach (var group in groups)
                {
                    if (cost.ContributingGroupCosts.TryGetValue(group, out var groupEntry))
                        cost.ContributingGroupCosts[group] = new ResourceCost([groupEntry, new ResourceCost(share, currency)]);
                    else
                        cost.ContributingGroupCosts[group] = new ResourceCost(share, currency);
                }
            }
        }

        return (projectCost, unassignedUsersCount == 0 ? null : new ResourceCost(licensePricePerUser * unassignedUsersCount, currency));
    }
}