namespace Mammon.Workflows.Activities;

public class CallResourceActorActivity(DaprClient client) : WorkflowActivity<CallResourceActorActivityRequest, string>
{
    public override async Task<string> RunAsync(WorkflowActivityContext context, CallResourceActorActivityRequest request)
    {
        var parentResourceId = request.Cost!.ResourceId.ToParentResourceId();
        var stateKey =  new string(parentResourceId.Where(char.IsLetterOrDigit).ToArray());

        var actorIdStateEntry = await client.GetStateAsync<string>(Consts.StateStoreName, stateKey);
        
        var actorGuid = string.Empty;
        bool isNewActorInstance = false;

        if (string.IsNullOrWhiteSpace(actorIdStateEntry))
        {
            actorGuid = Guid.NewGuid().ToString("N");
            await client.SaveStateAsync(Consts.StateStoreName, stateKey, actorGuid);
            isNewActorInstance = true;
        }
        else
        {
            actorGuid = actorIdStateEntry;
        }

        var actorId = $"ResourceActor{actorGuid}";

        if (isNewActorInstance)
        {
            await ActorProxy.DefaultProxyFactory.CallActoryWithNoTimeout<IResourceActor>(actorId, "ResourceActor", async (p) => await p.Initialize(parentResourceId, request.Cost.Tags));
        }
        await ActorProxy.DefaultProxyFactory.CallActoryWithNoTimeout<IResourceActor>(actorId, "ResourceActor", async (p) => await p.AddCostAsync(request.Cost!.ResourceId, request.Cost.Cost));

        return actorId;
    }
}
