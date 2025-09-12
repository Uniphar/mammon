namespace Mammon.Services;

public class LogAnalyticsService(
	ArmClient armClient,
	DefaultAzureCredential azureCredential,
	ILogger<LogAnalyticsService> logger) : BaseLogService
{
	public async Task<(IEnumerable<LAWorkspaceQueryResponseItem>, bool workspaceFound)> CollectUsageData(string laResourceId, DateTime from, DateTime to)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(laResourceId);

        Response<IReadOnlyList<LAWorkspaceQueryResponseItem>> response;

        try
		{
#if (DEBUG || INTTEST)

            response = await ParseMockFileAsync<LAWorkspaceQueryResponseItem>(Consts.MockLAQueryResponseFilePathConfigKey, laResourceId);
#else
			LogsQueryClient client = new(azureCredential);

            Response<OperationalInsightsWorkspaceResource>? workspace;

            workspace = await armClient.GetOperationalInsightsWorkspaceResource(new ResourceIdentifier(laResourceId)).GetAsync();

            if (workspace == null || !workspace.Value.HasData || workspace.Value.Data.CustomerId == null)
            {
                return ([], false);
            }

            response = await client.QueryWorkspaceAsync<LAWorkspaceQueryResponseItem>(workspace.Value.Data.CustomerId.ToString(),
				@$"search * 
			| where $table !='AzureActivity' and not(isempty(_ResourceId)) and _BilledSize > 0
			| project
				Size=_BilledSize,
				Selector=iff(isempty(PodNamespace), _ResourceId, PodNamespace),
				SelectorType=iff(isempty(PodNamespace), 'ResourceId', 'Namespace')
			| summarize SizeSum=sum(Size) by Selector, SelectorType
			| order by Selector desc",
				new QueryTimeRange(from, to));
#endif

            if (response.GetRawResponse() == null || response.GetRawResponse().IsError)
			{
				return ([], false);
			}

			return (response.Value.Where(x => x.SelectorType != Consts.ResourceIdLAWorkspaceSelectorType || !x.SelectorIdentifier!.IsLogAnalyticsWorkspace()), true);
		}
		catch (RequestFailedException e)
		{
			logger.LogError(e, $"Workspace {laResourceId} not found");
			return ([], false);
		}
	}
}

