namespace Mammon.Services;

public class LogAnalyticsService(ArmClient armClient, DefaultAzureCredential azureCredential, ILogger<LogAnalyticsService> logger)
{
	public async Task<IEnumerable<LAWorkspaceQueryResponseItem>> CollectUsageData(string laResourceId, DateTime from, DateTime to)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(laResourceId);

		LogsQueryClient client = new(azureCredential);

		Response<OperationalInsightsWorkspaceResource>? workspace;
		try
		{
			workspace = await armClient.GetOperationalInsightsWorkspaceResource(new ResourceIdentifier(laResourceId)).GetAsync();
		}
		catch (RequestFailedException e)//LA cannot be found
		{
			logger.LogError(e, $"Error querying workspace for {laResourceId}");
			workspace = null;
		}

		if (workspace == null || !workspace.Value.HasData || workspace.Value.Data.CustomerId == null)
		{
			return [];
		}

		var response = await client.QueryWorkspaceAsync<LAWorkspaceQueryResponseItem>(workspace.Value.Data.CustomerId.ToString(),
			@$"search * 
			| where $table !='AzureActivity' and not(isempty(_ResourceId)) and _BilledSize > 0
			| project
				Size=_BilledSize,
				Selector=iff(isempty(PodNamespace), _ResourceId, PodNamespace),
				SelectorType=iff(isempty(PodNamespace), 'ResourceId', 'Namespace')
			| summarize SizeSum=sum(Size) by Selector, SelectorType
			| order by Selector desc",
			new QueryTimeRange(from, to));

		if (response.GetRawResponse() == null || response.GetRawResponse().IsError)
		{
			throw new InvalidOperationException($"Error querying workspace for {laResourceId}");
		}

		return response.Value.Where(x => x.SelectorType != Consts.ResourceIdLAWorkspaceSelectorType || x.SelectorIdentifier!.IsLogAnalyticsWorkspace());
	}
}
