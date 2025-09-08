using Mammon.Models;

namespace Mammon.Services;

public class SQLPoolService(
	ArmClient armClient,
	LogsQueryClient logsQueryClient,
	IConfiguration configuration,
	ILogger<SQLPoolService> logger)
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

			if (dbs.Count == 0)
			{
				logger.LogInformation($"No databases found in SQL Pool {poolResourceId}");
				return ([], false);
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

			Response<IReadOnlyList<SQLDatabaseUsageResponseItem>> result;

#if (DEBUG || INTTEST)
			var mockedResponse = await File.ReadAllTextAsync(configuration[Consts.MockSqlPoolResponseFilePathConfigKey]!);
			var items = JsonSerializer.Deserialize<List<SQLDatabaseUsageResponseItem>>(mockedResponse)!;
			result = Response.FromValue<IReadOnlyList<SQLDatabaseUsageResponseItem>>(
				items,
				new MockResponse(200)
			);
#else
			string query = @$"AzureMetrics 
				| where MetricName=='dtu_used' and ResourceId in~ ({string.Join(",", dbs)})
				| summarize DTUAverage=avg(Average) by ResourceId
				| where DTUAverage>0";

            result = await logsQueryClient.QueryWorkspaceAsync<SQLDatabaseUsageResponseItem>(workspace.Value.Data.CustomerId.ToString(),
				query,
				new QueryTimeRange(from, to));
#endif

            return (result.Value, true);
		}
		catch (Exception ex) 
		{
			logger.LogError(ex, $"Error querying SQL Pool {poolResourceId}");
			return ([], false);
		}
	}
}
