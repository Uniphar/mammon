namespace Mammon.Actors;

public interface IMySQLServerActor : IActor
{
	Task SplitCost(SplittableResourceRequest request, IEnumerable<MySQLUsageResponseItem> data);
}
