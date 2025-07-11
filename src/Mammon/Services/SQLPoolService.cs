namespace Mammon.Services;

public class SQLPoolService(ArmClient armClient, LogsQueryClient logsQueryClient, ILogger<SQLPoolService> logger)
{
	public async Task<(IEnumerable<SQLDatabaseUsageResponseItem> usageData, bool successFlag)> ObtainQueryUsage(string poolResourceId, DateTime from, DateTime to)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(poolResourceId);

		try
		{

			//lookup pool LA id
			var rID = new ResourceIdentifier(poolResourceId);

			List<string> dbs = [];

			var pool = await armClient.GetElasticPoolResource(rID).GetAsync();
			await foreach (var db in pool.Value.GetDatabasesAsync())
			{
				dbs.Add($"'{db.Id}'");
			}

			var diagSetttings = armClient.GetDiagnosticSettings(pool.Value.Id);
			if (diagSetttings == null || !diagSetttings.Any())
				return ([], false);

			ResourceIdentifier? laWorkspaceId = diagSetttings.First().Data?.WorkspaceId;
			if (laWorkspaceId is null)
			{
				return ([], false);
			}
			var workspace = await armClient.GetOperationalInsightsWorkspaceResource(laWorkspaceId).GetAsync();

			string query = @$"AzureMetrics 
				| where MetricName=='dtu_used' and ResourceId in~ ({string.Join(",", dbs)})
				| summarize DTUAverage=avg(Average) by ResourceId
				| where DTUAverage>0";

			var result = await logsQueryClient.QueryWorkspaceAsync<SQLDatabaseUsageResponseItem>(workspace.Value.Data.CustomerId.ToString(),
				query,
				new QueryTimeRange(from, to));

			return (result.Value, true);
		}
		catch (Exception ex) 
		{
			logger.LogError(ex, $"Error querying SQL Pool {poolResourceId}");
			return ([], false);
		}
	}
}
