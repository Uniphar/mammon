namespace Mammon.Actors;

public interface ISQLPoolActor : IActor
{
	public Task SplitCost(string reportId, string resourceId, ResourceCost totalCost, IEnumerable<SQLDatabaseUsageResponseItem> data);
}
