namespace Mammon.Services;

public class LogAnalyticsService(ArmClient armClient, DefaultAzureCredential azureCredential, ILogger<LogAnalyticsService> logger)
{
    public async Task<(List<LAWorkspaceQueryResponseItem>, bool workspaceFound)> CollectUsageData(string laResourceId, DateTime from, DateTime to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laResourceId);

        LogsQueryClient client = new(azureCredential);

        try
        {
            var workspace = await armClient.GetOperationalInsightsWorkspaceResource(new ResourceIdentifier(laResourceId)).GetAsync();

            if (workspace == null || !workspace.Value.HasData || workspace.Value.Data.CustomerId == null)
            {
                return ([], false);
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
                return ([], false);
            }

            var laWorkspaceQueryResponseItems = response.Value
                .Where(x => x.SelectorType != Consts.ResourceIdLAWorkspaceSelectorType || !x.SelectorIdentifier!.IsLogAnalyticsWorkspace())
                .ToList();
            return (laWorkspaceQueryResponseItems, true);
        }
        catch (RequestFailedException e)
        {
            logger.LogError(e, $"Workspace {laResourceId} not found");
            return ([], false);
        }
    }
}