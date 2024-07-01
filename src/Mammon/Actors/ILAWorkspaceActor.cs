namespace Mammon.Actors;

public interface ILAWorkspaceActor : IActor
{
	public Task SplitCost(SplittableResourceRequest request, IEnumerable<LAWorkspaceQueryResponseItem> data);
}
