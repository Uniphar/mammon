namespace Mammon.Workflows.Activities;

public class AssignCostCentreActivity(ILogger<AssignCostCentreActivity> logger) : WorkflowActivity<AssignCostCentreActivityRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, AssignCostCentreActivityRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);

		try
		{
            //assign cost centre
            (string costCentre, ResourceCost cost) = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IResourceActor, (string costCentre, ResourceCost cost)>(input.ResourceActorId, nameof(ResourceActor), async (p) => await p.AssignCostCentre());

            //send them to cost centre actors
            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
                CostCentreActor.GetActorId(input.ReportId, costCentre, input.SubscriptionId),
                nameof(CostCentreActor),
                async (p) => await p.AddCostAsync(input.ResourceId, cost));

            return true;
        }
		catch (Exception ex)
		{
            logger.LogError(ex, "Error assigning cost centre for ReportId: {ReportId}, ResourceId: {ResourceId}", input.ReportId, input.ResourceId);
            throw;
		}
    }
}
