namespace Mammon.Utils;

public class AzureDevOpsAuthHandler : DelegatingHandler
{
    protected const string AdoAppId = "499b84ac-1321-427f-aa17-267ca6975798";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenResponse = await new DefaultAzureCredential().GetTokenAsync(new TokenRequestContext([$"{AdoAppId}/.default"]), cancellationToken);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
