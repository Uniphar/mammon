namespace Mammon.Workflows.Activities.DevOps;

public class ObtainPagedDevOpsMembershipEntitlementsActivity(AzureDevOpsClient azureDevOpsClient)
    : WorkflowActivity<PaginatedUserEntitlementsRequest, PaginatedUserEntitlementsResult>
{
    public override async Task<PaginatedUserEntitlementsResult> RunAsync(
        WorkflowActivityContext context, PaginatedUserEntitlementsRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.DevOpsOrganization)) return new PaginatedUserEntitlementsResult();
        
        var pageResponse = await azureDevOpsClient.GetMembersEntitlementsAsync(input.DevOpsOrganization, input.ContinuationToken);
        return pageResponse;
    }
}
