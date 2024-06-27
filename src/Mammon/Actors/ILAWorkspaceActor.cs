namespace Mammon.Actors;

public interface ILAWorkspaceActor : IActor
{
	public Task SplitCost(string reportId, string resourceId, ResourceCost totalCost, IEnumerable<LAWorkspaceQueryResponseItem> data);
}
