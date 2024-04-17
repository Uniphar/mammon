namespace Mammon.Extensions;

public static class ProxyFactorExtensions
{
    public static TActorInterface CreateActorProxyNoTimeout<TActorInterface>(this IActorProxyFactory actorProxyFactory, ActorId actorId, string actorType) where TActorInterface: IActor
    {
        return actorProxyFactory.CreateActorProxy<TActorInterface>(actorId, actorType, new ActorProxyOptions { RequestTimeout = Timeout.InfiniteTimeSpan });
    }
}
