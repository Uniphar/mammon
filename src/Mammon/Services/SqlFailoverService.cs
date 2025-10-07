using Azure.ResourceManager.Sql.Models;

namespace Mammon.Services;

public class SqlFailoverService(ArmClient armClient, ILogger<SqlFailoverService> logger)
{
    public async Task<string?> ResolvePrimaryPoolResourceIdAsync(string secondaryPoolResourceId)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secondaryPoolResourceId, nameof(secondaryPoolResourceId));

            var poolId = new ResourceIdentifier(secondaryPoolResourceId);
            var pool = armClient.GetElasticPoolResource(poolId);
            var poolResponse = await pool.GetAsync();
            var poolName = poolResponse.Value.Data.Name;

            var serverId = poolId.Parent;
            var server = armClient.GetSqlServerResource(serverId);

            var poolDbNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach (var db in pool.GetDatabasesAsync())
                poolDbNames.Add(db.Data.Name);

            if (poolDbNames.Count == 0)
            {
                logger.LogInformation("No databases found in secondary pool {PoolId}", secondaryPoolResourceId);
                return null;
            }

            var allDatabases = new List<SqlDatabaseResource>();
            await foreach (var db in server.GetSqlDatabases().GetAllAsync())
                allDatabases.Add(db);

            var dbsByFailoverGroup = allDatabases
                .Where(db => db.Data.FailoverGroupId is not null)
                .GroupBy(db => db.Data.FailoverGroupId!.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(db => db.Data.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));

            await foreach (var fg in server.GetFailoverGroups().GetAllAsync())
            {
                if (!dbsByFailoverGroup.TryGetValue(fg.Id.ToString(), out var fgDbNames))
                    continue;

                if (!poolDbNames.Overlaps(fgDbNames))
                    continue;

                var primaryPartner = fg.Data.PartnerServers
                    .FirstOrDefault(p => p.ReplicationRole == FailoverGroupReplicationRole.Primary);

                if (primaryPartner is null)
                {
                    logger.LogWarning(
                        "Failover group {FailoverGroupName} matched databases but has no primary partner",
                        fg.Data.Name);
                    continue;
                }

                var primaryPoolId = $"{primaryPartner.Id}/elasticPools/{poolName}";
                logger.LogInformation(
                    "Resolved primary pool {PrimaryPoolId} for secondary {SecondaryPoolId} via failover group {FailoverGroupName}",
                    primaryPoolId, secondaryPoolResourceId, fg.Data.Name);

                return primaryPoolId;
            }

            logger.LogInformation("No matching failover group found for {PoolId}", secondaryPoolResourceId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve primary pool for {SecondaryPoolId}", secondaryPoolResourceId);
            return null;
        }
    }
}
