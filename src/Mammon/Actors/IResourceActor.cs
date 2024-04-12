using Dapr.Actors;

namespace Mammon.Actors
{
    public interface IResourceActor : IActor
    {
        Task AddCostAsync(double cost, string[]? tags);
    }
}
