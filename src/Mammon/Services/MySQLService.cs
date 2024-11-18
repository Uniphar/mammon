namespace Mammon.Services;

public class MySQLService(ArmClient armClient, DefaultAzureCredential credential, IConfiguration configuration, ILogger<MySQLService> logger)
{
	public async Task<(IEnumerable<MySQLUsageResponseItem> usageData, bool successFlag)> ObtainQueryUsage(string mySQLServerID, DateTime from, DateTime to)
	{
		try
		{
			var (connection, validationSuccess) = await GetServerConnection(mySQLServerID);
			if (!validationSuccess || connection == null)
			{
				return ([], false);
			}

			if (!connection.State.HasFlag(ConnectionState.Open))
				await connection.OpenAsync();

			MySqlCommand cmd = connection.CreateCommand();
			cmd.CommandText = "SELECT table_schema AS DBName, SUM(data_length + index_length) AS DBSize FROM information_schema.tables WHERE table_schema NOT IN ('mysql', 'information_schema', 'performance_schema') GROUP BY table_schema";
			MySqlDataReader reader = await cmd.ExecuteReaderAsync();
		
			List<MySQLUsageResponseItem> result = [];

			while (await reader.ReadAsync())
			{
				result.Add(new MySQLUsageResponseItem
				{
					DBName = reader.GetString(0),
					DBSize = reader.GetInt64(1),
				});
			}
		

			return (result, true);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Error querying MySQL resource {mySQLServerID}");
			return ([], false);
		}
	}

	private async Task<(MySqlConnection? connection, bool validationSuccess)> GetServerConnection(string mySQLServerID)
	{
		var mySQLServerRID = new ResourceIdentifier(mySQLServerID);

		var resourceGroupResource = await armClient.GetResourceGroupResource(mySQLServerRID.Parent).GetAsync();
		var gr = await armClient.GetGenericResource(mySQLServerRID).GetAsync();
		var server = await resourceGroupResource.Value.GetMySqlFlexibleServerAsync(gr.Value.Data.Name);//TODO: check flexible vs regular

		string? serverUrl;

		if (server.Value.Data.Network.PublicNetworkAccess== MySqlFlexibleServerEnableStatusEnum.Enabled)
		{
			serverUrl = server.Value.Data.FullyQualifiedDomainName;
		}
		else
		{
			var privateEndpoint = server.Value.Data.PrivateEndpointConnections.FirstOrDefault();
			if (privateEndpoint==null || privateEndpoint.ConnectionState.Status!= MySqlFlexibleServersPrivateEndpointServiceConnectionStatus.Approved)
			{
				return (null, false);
			}
			else
			{
				var privateEndpointResource = await armClient.GetPrivateEndpointResource(privateEndpoint.PrivateEndpointId).GetAsync();

				var ipConfiguration = privateEndpointResource?.Value?.Data.CustomDnsConfigs?.FirstOrDefault()?.IPAddresses?.FirstOrDefault();
				if (ipConfiguration == null)
				{
					return (null, false);
				}

				serverUrl = ipConfiguration;
			}
		}

		var token = await credential.GetTokenAsync(new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]));

		var userId = configuration.GetWorkloadIdentityName();
		if (string.IsNullOrWhiteSpace(userId))
		{
			return (null, false);
		}

		var csb = new MySqlConnectionStringBuilder()
		{
			Server = serverUrl,
			UserID = userId,
			Password = token.Token,
		};

		return (new MySqlConnection(csb.ConnectionString), true);
	}
}
