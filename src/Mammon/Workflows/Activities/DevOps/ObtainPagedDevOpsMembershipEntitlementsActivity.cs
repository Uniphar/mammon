namespace Mammon.Workflows.Activities.DevOps;

public class ObtainPagedDevOpsMembershipEntitlementsActivity(AzureDevOpsClient azureDevOpsClient)
    : WorkflowActivity<PaginatedMemberEntitlementsRequest, PaginatedMemberEntitlementsResult>
{
    public override async Task<PaginatedMemberEntitlementsResult> RunAsync(
        WorkflowActivityContext context, PaginatedMemberEntitlementsRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.DevOpsOrganization)) return new PaginatedMemberEntitlementsResult();
        
        var pageResponse = await azureDevOpsClient.GetMembersEntitlementsAsync(input.DevOpsOrganization, input.ContinuationToken);
        return pageResponse;
    }
}
