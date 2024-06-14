namespace Mammon.Services;

public class LogAnalyticsService(ArmClient armClient, DefaultAzureCredential azureCredential)
{
	public async Task<IEnumerable<LAWorkspaceQueryResponseItem>> CollectUsageData(string laResourceId, DateTime from, DateTime to)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(laResourceId);

		LogsQueryClient client = new(azureCredential);

		var workspace = await armClient.GetOperationalInsightsWorkspaceResource(new ResourceIdentifier(laResourceId)).GetAsync();

		if (workspace == null || !workspace.Value.HasData || workspace.Value.Data.CustomerId == null)
		{
			throw new InvalidOperationException($"Workspace not found for {laResourceId}");
		}

		var response = await client.QueryWorkspaceAsync<LAWorkspaceQueryResponseItem>(workspace.Value.Data.CustomerId.ToString(),
			@$"search * 
			| where $table !='AzureActivity' and _ResourceId <>'' and _BilledSize > 0
			| project
				Size=_BilledSize,
				Selector=iff(PodNamespace != '', PodNamespace, _ResourceId),
				SelectorType=iff(PodNamespace != '', 'Namespace', 'ResourceId')
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
