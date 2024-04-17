namespace Mammon.Extensions;

public static class PolicyBuilderExtensions
{
    private const string retryAfterHeader = "x-ms-ratelimit-microsoft.costmanagement-clienttype-retry-after";

    public static AsyncRetryPolicy<HttpResponseMessage> AddCostManagementRetryPolicy(this PolicyBuilder<HttpResponseMessage> builder)
    {
        return builder.WaitAndRetryAsync(3, 
            (i, resp, ctx) => {
                if (resp.Result.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var header = resp.Result.Headers.GetValues(retryAfterHeader)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(header))
                        return TimeSpan.FromSeconds(Convert.ToDouble(header));
                }

                return TimeSpan.FromMinutes(Math.Pow(2, i));
            }
            ,(resp, ts, i, ctx) => {
                return Task.CompletedTask;
            });
    }
}
