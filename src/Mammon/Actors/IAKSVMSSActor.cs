namespace Mammon.Actors;

public interface IAKSVMSSActor : IActor
{
	public Task SplitCost(SplittableResourceRequest request, IEnumerable<AKSVMSSUsageResponseItem> data);
}
