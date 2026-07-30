
namespace Mammon.Services;


public class StateService(CosmosClient cosmosClient)
{
    private CosmosClient cosmosClient = cosmosClient;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Dapr's Azure CosmosDB actor state store component (mammon-actor-state) requires this container to pre-exist with partition key path "/partitionKey".
        var actorStateStoreDatabase = await cosmosClient.CreateDatabaseIfNotExistsAsync("platform", cancellationToken: cancellationToken);
        await actorStateStoreDatabase.Database.CreateContainerIfNotExistsAsync("mammon-actor-state", "/partitionKey", cancellationToken: cancellationToken);
    }
}