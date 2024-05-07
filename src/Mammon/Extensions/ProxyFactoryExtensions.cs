namespace Mammon.Extensions;

public static class ProxyFactoryExtensions
{
    public static TActorInterface CreateActorProxyNoTimeout<TActorInterface>(this IActorProxyFactory actorProxyFactory, string actorId, string actorType) where TActorInterface: IActor
    {
        return actorProxyFactory.CreateActorProxy<TActorInterface>(new ActorId(actorId), actorType, new ActorProxyOptions { RequestTimeout = Timeout.InfiniteTimeSpan });
    }

    public static async Task CallActorWithNoTimeout<TActorInterface>(this IActorProxyFactory actorProxyFactory, string actorId, string actorType, Func<TActorInterface, Task> action ) where TActorInterface : IActor
    {
        var proxy =  actorProxyFactory.CreateActorProxyNoTimeout<TActorInterface>(actorId, actorType);
        await action(proxy);
    }

    public static async Task<TResponse> CallActorWithNoTimeout<TActorInterface, TResponse>(this IActorProxyFactory actorProxyFactory, string actorId, string actorType, Func<TActorInterface, Task<TResponse>> action) where TActorInterface : IActor
    {
        var proxy = actorProxyFactory.CreateActorProxyNoTimeout<TActorInterface>(actorId, actorType);
        return await action(proxy);
    }
}
