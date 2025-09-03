namespace Mammon.Services;

public class VDIService(ArmClient armClient, DefaultAzureCredential azureCredential, ILogger<VDIService> logger)
{
	public async Task<(List<VDIQueryUsageResponseItem> usageData, bool dataAvailable)> ObtainQueryUsage(string resourceGroupId, DateTime from, DateTime to)
	{
		try
		{
			//find hostpool
			var hostPools = armClient.GetResourceGroupResource(new ResourceIdentifier(resourceGroupId)).GetHostPools();

			var hostPool = hostPools.First();

			var diagSettings = armClient.GetDiagnosticSettings(hostPool.Id).GetAll();
			if (diagSettings==null || !diagSettings.Any())
			{
				return ([], false);
			}

			var diagSetting = diagSettings.First();
			var diagSettingsResource = diagSetting.Get();
			var workspaceId = diagSettingsResource.Value.Data.WorkspaceId;
			var workspace = await armClient.GetOperationalInsightsWorkspaceResource(workspaceId).GetAsync();

			//query session usage
			LogsQueryClient client = new(azureCredential);

			var response = await client.QueryWorkspaceAsync<VDIQueryUsageResponseItem>(workspace.Value.Data.CustomerId.ToString(),
					@$"WVDConnections
					| where _ResourceId =~'{hostPool.Id}' and State == 'Completed' and ResourceAlias has '#'
					| extend GroupID = tostring(split(ResourceAlias, '#', 0)[0])
					| summarize SessionCount=count() by GroupID
					| where SessionCount > 0",
					new QueryTimeRange(from, to));


			return (response.Value.ToList(), true);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Error querying VDI Host Pool for RG {resourceGroupId}");
			return ([], false);
		}
	}
}
