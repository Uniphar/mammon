namespace Mammon.Actors;

public interface IAKSVMSSActor : IActor
{
	public Task SplitCost(string reportId, string resourceId, ResourceCost totalCost, IEnumerable<AKSVMSSUsageResponseItem> data);
}
