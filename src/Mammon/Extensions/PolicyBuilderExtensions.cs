namespace Mammon.Extensions;

public static class PolicyBuilderExtensions
{
    private const string EntityRetryAfterHeader = "x-ms-ratelimit-microsoft.costmanagement-entity-retry-after";
    private const string ClientTypeRetryAfterHeader = "x-ms-ratelimit-microsoft.costmanagement-clienttype-retry-after";

    public static AsyncRetryPolicy<HttpResponseMessage> AddCostManagementRetryPolicy(
        this PolicyBuilder<HttpResponseMessage> builder)
    {
        return builder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (i, resp, ctx) =>
            {
                if (resp.Result.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var header = GetHeaderValue(resp.Result, EntityRetryAfterHeader)
                                 ?? GetHeaderValue(resp.Result, ClientTypeRetryAfterHeader);

                    if (!string.IsNullOrWhiteSpace(header) && double.TryParse(header, out var seconds))
                    {
                        return TimeSpan.FromSeconds(seconds);
                    }
                }

                return TimeSpan.FromMinutes(Math.Pow(2, i));
            },
            onRetryAsync: (resp, ts, i, ctx) => Task.CompletedTask
        );
    }

    private static string? GetHeaderValue(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            ? values.FirstOrDefault()
            : null;
    }
}

