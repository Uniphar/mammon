namespace Mammon.Services;

public class LogAnalyticsService(
    ArmClient armClient,
    DefaultAzureCredential azureCredential,
    ILogger<LogAnalyticsService> logger) : BaseLogService
{
    private const int DaysPerChunk = 10;

    private const string UsageQuery = @"
        search *
        | where $table != 'AzureActivity' and not(isempty(_ResourceId)) and _BilledSize > 0
        | project
            Size=_BilledSize,
            Selector=iff(isempty(PodNamespace), _ResourceId, PodNamespace),
            SelectorType=iff(isempty(PodNamespace), 'ResourceId', 'Namespace')
        | summarize SizeSum=sum(Size) by Selector, SelectorType
        | order by Selector desc";

    public async Task<(IEnumerable<LAWorkspaceQueryResponseItem>, bool workspaceFound)> CollectUsageData(
        string laResourceId,
        DateTime from,
        DateTime to)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laResourceId);

#if (DEBUG || INTTEST)
        var mock = await ParseMockFileAsync<LAWorkspaceQueryResponseItem>(
            Consts.MockLAQueryResponseFilePathConfigKey,
            laResourceId);

        if (mock.GetRawResponse() == null || mock.GetRawResponse().IsError)
            return ([], false);

        return (mock.Value, true);
#else
        try
        {
            var workspaceResponse = await armClient
                .GetOperationalInsightsWorkspaceResource(new ResourceIdentifier(laResourceId))
                .GetAsync();

            var workspace = workspaceResponse.Value;

            if (!workspace.HasData || workspace.Data.CustomerId is null)
                return ([], false);

            var customerId = workspace.Data.CustomerId.ToString();

            var client = new LogsQueryClient(azureCredential);
            var allItems = new List<LAWorkspaceQueryResponseItem>();

            foreach (var (chunkFrom, chunkTo) in SplitIntoChunksInclusive(from, to, DaysPerChunk))
            {
                var resp = await client.QueryWorkspaceAsync<LAWorkspaceQueryResponseItem>(
                    customerId,
                    UsageQuery,
                    new QueryTimeRange(chunkFrom, chunkTo));

                if (resp.GetRawResponse() == null || resp.GetRawResponse().IsError)
                    return ([], false);

                allItems.AddRange(resp.Value);
            }

            return (MergeAndFilter(allItems), true);
        }
        catch (RequestFailedException e)
        {
            logger.LogError(e, $"Workspace {laResourceId} not found");
            return ([], false);
        }
#endif
    }

    /// <summary>
    /// Example:
    /// from = 2026-02-01 00:00:00
    /// to   = 2026-02-28 23:59:59
    /// Produces:
    /// 01 00:00:00 -> 10 23:59:59
    /// 11 00:00:00 -> 20 23:59:59
    /// 21 00:00:00 -> 28 23:59:59
    /// </summary>
    private static IEnumerable<(DateTime from, DateTime to)> SplitIntoChunksInclusive(
        DateTime from,
        DateTime to,
        int daysPerChunk)
    {
        var cursorStart = from;

        while (cursorStart <= to)
        {
            var cursorEnd = cursorStart.AddDays(daysPerChunk).AddSeconds(-1);

            if (cursorEnd > to)
                cursorEnd = to;

            yield return (cursorStart, cursorEnd);

            cursorStart = cursorEnd.AddSeconds(1);
        }
    }

    private static IEnumerable<LAWorkspaceQueryResponseItem> MergeAndFilter(
        IEnumerable<LAWorkspaceQueryResponseItem> items)
    {
        var filtered = items.Where(x =>
            x.SelectorType != Consts.ResourceIdLAWorkspaceSelectorType ||
            x.SelectorIdentifier is null ||
            !x.SelectorIdentifier.IsLogAnalyticsWorkspace());

        return filtered
            .GroupBy(
                x => (x.SelectorType, x.Selector),
                SelectorKeyComparer.Instance)
            .Select(g => new LAWorkspaceQueryResponseItem
            {
                SelectorType = g.Key.SelectorType,
                Selector = g.Key.Selector,
                SizeSum = g.Sum(x => x.SizeSum)
            })
            .OrderByDescending(x => x.SizeSum);
    }

    private sealed class SelectorKeyComparer : IEqualityComparer<(string SelectorType, string Selector)>
    {
        public static readonly SelectorKeyComparer Instance = new();

        public bool Equals(
            (string SelectorType, string Selector) x,
            (string SelectorType, string Selector) y)
            => string.Equals(x.SelectorType, y.SelectorType, StringComparison.Ordinal) &&
               string.Equals(x.Selector, y.Selector, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string SelectorType, string Selector) obj)
            => HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.SelectorType ?? string.Empty),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Selector ?? string.Empty));
    }
}