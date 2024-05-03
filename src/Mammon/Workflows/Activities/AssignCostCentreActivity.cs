namespace Mammon.Workflows.Activities;

public class AssignCostCentreActivity : WorkflowActivity<AssignCostCentreActivityRequest, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, AssignCostCentreActivityRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);

        //get cost centre costs
        var costCentreCosts = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IResourceActor, IDictionary<string, double>>(input.ResourceActorId, "ResourceActor", async (p) => await p.AssignCostCentreCosts()) 
            ?? throw new InvalidOperationException($"Unexpected result returned for ${input.ResourceId} - AssignCostCentreCosts");

        foreach (var (costCentre, costCentreCost) in costCentreCosts)
        {            
            //send them to cost centre actors
            await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>($"{input.ReportId}_{costCentre}", "CostCentreActor", async (p) => await p.AddCostAsync(input.ResourceId, costCentreCost));
        }

        return true;
    }
}
