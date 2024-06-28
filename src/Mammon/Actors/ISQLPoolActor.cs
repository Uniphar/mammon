namespace Mammon.Actors;

public interface ISQLPoolActor : IActor
{
	public Task SplitCost(SplittableResourceRequest request, IEnumerable<SQLDatabaseUsageResponseItem> data);
}
