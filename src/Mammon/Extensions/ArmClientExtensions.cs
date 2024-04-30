namespace Mammon.Services;

public static class ArmClientExtensions
{
    public static async Task<IDictionary<string, string>?> GetTags(this ArmClient armClient, string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId, nameof(resourceId));

        //only resources in subcriptions are supported for Tag API
        var rId = new ResourceIdentifier(resourceId);
        if (string.IsNullOrWhiteSpace(rId.SubscriptionId))
        {
            return null;
        }    

        var data = await armClient.ExecuteWithRetries<GenericResource>(async (c) =>
        {
            var response =  await c.GetGenericResource(rId).GetAsync();
            return response;
        });

        var rawResponse = data.GetRawResponse();

        if (rawResponse.Status!=(int) HttpStatusCode.NotFound && rawResponse.IsError)
        {
            throw new InvalidOperationException($"Unable to retrieve tags for {resourceId}");
        }

        return rawResponse.Status == (int)HttpStatusCode.NotFound  ? null : data.Value.Data.Tags;
    }

    public static async Task<Response<TResponseType>> ExecuteWithRetries<TResponseType>(this ArmClient armClient, Func<ArmClient, Task<Response<TResponseType>>> execute)
    {
        return await Policy
            .HandleResult<Response<TResponseType>>((r) => r.GetRawResponse()?.Status == (int)HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(10, (i) => TimeSpan.FromSeconds(Math.Pow(2, i)))
            .ExecuteAsync(async () =>
            {
                return await execute(armClient);
            });
    }
}
