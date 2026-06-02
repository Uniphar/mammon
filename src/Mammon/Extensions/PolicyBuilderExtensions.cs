namespace Mammon.Extensions;

public static class PolicyBuilderExtensions
{
    private const string ConsumptionRetryAfter = "x-ms-ratelimit-microsoft.consumption-retry-after";
    private const string ServiceUnavailableRetryAfter = "Retry-After";

    public static AsyncRetryPolicy<HttpResponseMessage> AddCostManagementRetryPolicy(
        this PolicyBuilder<HttpResponseMessage> builder)
    {
        return builder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (i, resp, ctx) =>
            {
                if (resp.Result.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var header = GetHeaderValue(resp.Result, ConsumptionRetryAfter)
                                 ?? GetHeaderValue(resp.Result, ServiceUnavailableRetryAfter);

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

