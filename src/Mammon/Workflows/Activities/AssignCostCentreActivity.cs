namespace Mammon.Workflows.Activities;

public class AssignCostCentreActivity : WorkflowActivity<AssignCostCentreActivityRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, AssignCostCentreActivityRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);

        //assign cost centre
        (string costCentre, ResourceCost cost) = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IResourceActor, (string costCentre, ResourceCost cost)>(input.ResourceActorId, nameof(ResourceActor), async (p) => await p.AssignCostCentre());            

        //send them to cost centre actors
        await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(input.ReportId, costCentre), nameof(CostCentreActor), async (p) => await p.AddCostAsync(input.ResourceId, cost));

        return true;
    }
}
