
using Azure.Core;
using Azure.Identity;

namespace MammonActors.Utils
{
    public class AzureAuthHandler :DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await new DefaultAzureCredential().GetTokenAsync(new TokenRequestContext(scopes: ["https://management.azure.com/.default"]), cancellationToken);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
