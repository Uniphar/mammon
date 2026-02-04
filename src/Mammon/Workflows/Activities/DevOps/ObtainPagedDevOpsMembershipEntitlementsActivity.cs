namespace Mammon.Workflows.Activities.DevOps;

public class ObtainPagedDevOpsMembershipEntitlementsActivity(
    AzureDevOpsClient azureDevOpsClient,
    ILogger<ObtainPagedDevOpsMembershipEntitlementsActivity> logger) : WorkflowActivity<PaginatedMemberEntitlementsRequest, PaginatedMemberEntitlementsResult>
{
    public override async Task<PaginatedMemberEntitlementsResult> RunAsync(
        WorkflowActivityContext context, PaginatedMemberEntitlementsRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.DevOpsOrganization)) return new PaginatedMemberEntitlementsResult();

        try
        {
            var pageResponse = await azureDevOpsClient.GetMembersEntitlementsAsync(input.DevOpsOrganization, input.ContinuationToken);
            return pageResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obtaining DevOps membership entitlements for Organization: {DevOpsOrganization}, ContinuationToken: {ContinuationToken}",
                input.DevOpsOrganization, input.ContinuationToken);
            throw;
        }
    }
}
