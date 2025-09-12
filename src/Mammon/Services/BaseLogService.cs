using Mammon.Models;

namespace Mammon.Services;

public abstract class BaseLogService
{
    protected async Task<Response<IReadOnlyList<T>>> ParseMockFileAsync<T>(
        string mockFilePath,
        string resourceIdKey)
    {
        var mockedResponse = await File.ReadAllTextAsync(mockFilePath);

        using var doc = JsonDocument.Parse(mockedResponse);

        var subscriptionId = resourceIdKey.GetSubscriptionId();
        var subscriptionJson = doc.RootElement.GetProperty(subscriptionId).GetRawText();

        var items = JsonSerializer.Deserialize<List<T>>(subscriptionJson)!;
        return Response.FromValue<IReadOnlyList<T>>(
            items,
            new MockResponse(200)
        );
    }
}