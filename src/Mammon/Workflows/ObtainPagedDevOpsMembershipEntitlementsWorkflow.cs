namespace Mammon.Workflows;

public class ObtainPagedDevOpsMembershipEntitlementsWorkflow : Workflow<PaginatedUserEntitlementsRequest, PaginatedUserEntitlementsResult>
{
    public override async Task<PaginatedUserEntitlementsResult> RunAsync(
        WorkflowContext context, PaginatedUserEntitlementsRequest input)
    {
        var result = await context.CallActivityAsync<PaginatedUserEntitlementsResult>(
            nameof(ObtainPagedDevOpsMembershipEntitlementsActivity),
            input);

        return result;
    }
}
