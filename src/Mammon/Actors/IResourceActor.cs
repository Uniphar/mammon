namespace Mammon.Actors;

public interface IResourceActor : IActor
{
    Task AddCostAsync(string costId, double cost, string[]? tags);
}
