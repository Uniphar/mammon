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
            sleepDurationProvider: (attempt, outcome, context) =>
            {
                if (outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout } response)
                {
                    var header = GetHeaderValue(response, ConsumptionRetryAfter)
                                 ?? GetHeaderValue(response, ServiceUnavailableRetryAfter);

                    if (!string.IsNullOrWhiteSpace(header) &&
                        double.TryParse(header, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                    {
                        return TimeSpan.FromSeconds(seconds);
                    }
                }

                return TimeSpan.FromMinutes(Math.Pow(2, attempt));
            },
            onRetryAsync: (outcome, delay, attempt, context) => Task.CompletedTask);
    }

    private static string? GetHeaderValue(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            ? values.FirstOrDefault()
            : null;
    }
}