using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Mammon.Services;

public class AzureDevOpsClient : IDisposable
{
    private readonly ILogger<AzureDevOpsClient> _logger;
    private readonly HttpClient _httpClient;

    public AzureDevOpsClient(ILogger<AzureDevOpsClient> logger, string patToken)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{patToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<PaginatedUserEntitlementsResult> GetMembersEntitlementsAsync(string organization, string? continuationToken =  null)
    {
        var allMembers = new List<MemberEntitlementItem>();
        _logger.LogInformation($"Fetching members from Azure DevOps organization: {organization}");
        
        var url = $"https://vsaex.dev.azure.com/{organization}/_apis/MemberEntitlements?select=license,projects&$orderBy=name Ascending";

        if (!string.IsNullOrEmpty(continuationToken))
        {
            url += $"&continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Failed to fetch members. Status: {response.StatusCode}, Error: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MemberEntitlementsResponse>(content);

            if (result?.Items != null)
            {
                allMembers.AddRange(result.Items);
            }

            continuationToken = result?.ContinuationToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching members: {ex.Message}");
            throw;
        }

        _logger.LogInformation($"Returned {allMembers.Count} members and their entitlements");
        return new PaginatedUserEntitlementsResult
        {
            Users = allMembers,
            ContinuationToken = continuationToken
        };
    }

    public class MemberEntitlementsResponse
    {
        [JsonProperty("items")]
        public List<MemberEntitlementItem> Items { get; set; } = new();

        [JsonProperty("continuationToken")]
        public string? ContinuationToken { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}