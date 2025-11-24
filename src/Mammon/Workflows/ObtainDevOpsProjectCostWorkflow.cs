namespace Mammon.Workflows;

public class ObtainDevOpsProjectCostWorkflow : Workflow<ObtainDevOpsProjectCostRequest, DevOpsProjectsCosts>
{
    public override async Task<DevOpsProjectsCosts> RunAsync(WorkflowContext context, ObtainDevOpsProjectCostRequest input)
    {
	    var allUsers = new List<MemberEntitlementItem>();
	    string? token = null;
	    do
	    {
		    var page = await context.CallChildWorkflowAsync<PaginatedUserEntitlementsResult>(
			    nameof(ObtainPagedDevOpsMembershipEntitlementsWorkflow),
			    new PaginatedUserEntitlementsRequest
			    {
				    DevOpsOrganization = input.DevOpsOrganization,
				    ContinuationToken = token
			    });

		    allUsers.AddRange(page.Users);
		    token = page.ContinuationToken;

	    } while (token != null);
        
        return await context.CallActivityAsync<DevOpsProjectsCosts>(nameof(DevOpsProjectCostsActivity),
	        new DevOpsMapProjectCostsActivityRequest
	        {
		        MemberEntitlements = allUsers,
		        LicenseCosts = input.LicenseCosts,
		        ReportId =  input.ReportId,
		        DevOpsOrganization =  input.DevOpsOrganization,
	        });
    }
}