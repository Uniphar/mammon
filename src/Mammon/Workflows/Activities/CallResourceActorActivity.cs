namespace Mammon.Workflows.Activities;

public class CallResourceActorActivity(
    DaprClient client, 
    IConfiguration configuration,
    ILogger<CallResourceActorActivity> logger) : WorkflowActivity<CallResourceActorActivityRequest, CallResourceActorActivityResponse>
{
	public override async Task<CallResourceActorActivityResponse> RunAsync(WorkflowActivityContext context, CallResourceActorActivityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return await RunInternalAsync(request);
        }
        catch (Exception)
        {
            logger.LogError("Error calling Resource Actor for ReportId: {ReportId}, ResourceId: {ResourceId}", request.ReportId, request.Cost?.ResourceId);
            throw;
        }
    }

    private async Task<CallResourceActorActivityResponse> RunInternalAsync(CallResourceActorActivityRequest request)
    {
        var storeName = configuration[Consts.StateStoreNameConfigKey];

        var parentResourceId = request.Cost!.ResourceId.ToParentResourceId();

        var stateKey = $"ResourceActorIdMap_{request.ReportId}_{new string(parentResourceId.Where(char.IsLetterOrDigit).ToArray())}";
        ///Actor ID has limitations as to the length, with some ResourceIds being long (AKS Node VHD Image as an example)
        ///we now resort to computing hash
        var stateKeyHash = stateKey.ToSHA256();

        var actorIdStateEntry = await client.GetStateAsync<string>(storeName, stateKeyHash);

        var actorGuid = string.Empty;

        if (string.IsNullOrWhiteSpace(actorIdStateEntry))
        {
            actorGuid = Guid.NewGuid().ToString("N");
            await client.SaveStateAsync(storeName, stateKey, actorGuid);
        }
        else
        {
            actorGuid = actorIdStateEntry;
        }

        var actorId = $"ResourceActor{actorGuid}";

        await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IResourceActor>(actorId, nameof(ResourceActor), async (p) => await p.AddCostAsync(request.Cost!.ResourceId, request.Cost.Cost, parentResourceId, request.Cost.Tags));

        return new CallResourceActorActivityResponse { ResourceActorId = actorId, ResourceId = parentResourceId };
    }
}
