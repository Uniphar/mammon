using Azure.ResourceManager.Sql.Models;

namespace Mammon.Services;

public class SqlFailoverService(
    ArmClient armClient,
    ILogger<SqlFailoverService> logger)
{
    public async Task<string?> ResolvePrimaryPoolResourceIdAsync(string secondaryPoolResourceId)
    {
        try
        {
            var poolId = new ResourceIdentifier(secondaryPoolResourceId);
            var pool = armClient.GetElasticPoolResource(poolId);

            var poolResponse = await pool.GetAsync();
            var poolName = poolResponse.Value.Data.Name;

            // Parent is the SQL server
            var serverId = poolId.Parent;
            var server = armClient.GetSqlServerResource(serverId);

            // Get all failover groups for this server
            FailoverGroupCollection fgCollection = server.GetFailoverGroups();

            await foreach (FailoverGroupResource fg in fgCollection.GetAllAsync())
            {
                // Each failover group has partner servers
                var primaryPartner = fg.Data.PartnerServers
                    .FirstOrDefault(p => p.ReplicationRole == FailoverGroupReplicationRole.Primary);

                if (primaryPartner is not null)
                {
                    // Build elastic pool id for the primary
                    var primaryPoolId = $"{primaryPartner.Id}/elasticPools/{poolName}";
                    return primaryPoolId;
                }
            }

            logger.LogInformation("No failover group found for {PoolId}", secondaryPoolResourceId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve primary pool for {SecondaryPoolId}", secondaryPoolResourceId);
            return null;
        }
    }
}

