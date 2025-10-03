namespace Mammon.Services;

public class SQLPoolService(
    ArmClient armClient,
    LogsQueryClient logsQueryClient,
    SqlFailoverService failoverService,
    ILogger<SQLPoolService> logger) : BaseLogService
{
    public async Task<(IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool successFlag)> ObtainQueryUsage(
        string poolResourceId,
        DateTime from,
        DateTime to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(poolResourceId);

        Response<IReadOnlyList<SQLDatabaseUsageResponseItem>>? result;

        try
        {
#if (DEBUG || INTTEST)

            result = await ParseMockFileAsync<SQLDatabaseUsageResponseItem>(
                Consts.MockSqlPoolResponseFilePathConfigKey,
                poolResourceId);

#else
            async Task<Response<IReadOnlyList<SQLDatabaseUsageResponseItem>>?> RunQuery(string targetPoolId)
            {
                var rID = new ResourceIdentifier(targetPoolId);

                List<string> dbs = [];

                var pool = await armClient.GetElasticPoolResource(rID).GetAsync();
                await foreach (var db in pool.Value.GetDatabasesAsync())
                {
                    dbs.Add($"'{db.Id}'");
                }

                if (dbs.Count == 0)
                {
                    logger.LogInformation("No databases found in SQL Pool {PoolId}", targetPoolId);
                    return null;
                }

                var diagSetttings = armClient.GetDiagnosticSettings(pool.Value.Id);
                if (diagSetttings == null || !diagSetttings.Any())
                    return null;

                ResourceIdentifier? laWorkspaceId = diagSetttings.First().Data?.WorkspaceId;
                if (laWorkspaceId is null)
                    return null;

                var workspace = await armClient
                    .GetOperationalInsightsWorkspaceResource(laWorkspaceId)
                    .GetAsync();

                string query = @$"AzureMetrics 
                    | where MetricName=='dtu_used' and ResourceId in~ ({string.Join(",", dbs)})
                    | summarize DTUAverage=avg(Average) by ResourceId
                    | where DTUAverage>0";

                return await logsQueryClient.QueryWorkspaceAsync<SQLDatabaseUsageResponseItem>(
                    workspace.Value.Data.CustomerId.ToString(),
                    query,
                    new QueryTimeRange(from, to));
            }

            // First try the requested pool
            result = await RunQuery(poolResourceId);
            if (result != null && result.Value.Count > 0)
                return (result.Value, true);

            // Fallback: try resolving primary
            var primaryPoolId = await failoverService.ResolvePrimaryPoolResourceIdAsync(poolResourceId);
            if (!string.IsNullOrWhiteSpace(primaryPoolId))
            {
                result = await RunQuery(primaryPoolId);
                if (result != null && result.Value.Count > 0)
                    return (result.Value, true);
            }

            logger.LogInformation(
                "No usage data found for SQL Pool {PoolId} (and its primary if applicable) between {From} and {To}",
                poolResourceId, from, to);

            return ([], false);
#endif

            return (result.Value, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying SQL Pool {PoolId}", poolResourceId);
            return ([], false);
        }
    }
}