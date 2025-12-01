namespace Mammon.Utils;

public class AzureAuthHandler :DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await new DefaultAzureCredential().GetTokenAsync(new TokenRequestContext(scopes: ["https://management.azure.com/.default"]), cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}

public class AzureDevOpsAuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string adoAppId = "499b84ac-1321-427f-aa17-267ca6975798";
        string[] scopes = [$"{adoAppId}/.default"];
        var credential = new DefaultAzureCredential();
        var context = new TokenRequestContext(scopes);

        AccessToken tokenResponse = await credential.GetTokenAsync(context, cancellationToken);
        string token = tokenResponse.Token;

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
