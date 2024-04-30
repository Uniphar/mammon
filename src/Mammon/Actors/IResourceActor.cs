namespace Mammon.Actors;

public interface IResourceActor : IActor
{
    Task Initialize(string resourceId, Dictionary<string, string> tags);
    Task AddCostAsync(string costId, double cost);
}
