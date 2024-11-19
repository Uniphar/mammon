namespace Mammon.Actors;

public interface IMySQLServerActor : IActor
{
	Task SplitCost(SplittableResourceRequest request, IDictionary<string, double> proRata);
}
