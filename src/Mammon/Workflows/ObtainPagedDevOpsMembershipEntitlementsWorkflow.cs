namespace Mammon.Workflows;

public class ObtainPagedDevOpsMembershipEntitlementsWorkflow : Workflow<PaginatedMemberEntitlementsRequest, PaginatedMemberEntitlementsResult>
{
    public override async Task<PaginatedMemberEntitlementsResult> RunAsync(
        WorkflowContext context, PaginatedMemberEntitlementsRequest input)
    {
        var result = await context.CallActivityAsync<PaginatedMemberEntitlementsResult>(
            nameof(ObtainPagedDevOpsMembershipEntitlementsActivity),
            input);

        return result;
    }
}
