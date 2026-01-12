namespace Mammon.Actors;

public interface IVisualStudioSubscriptionCostActor : IActor
{
	Task SplitCostAsync(VisualStudioSubscriptionsSplittableResourceRequest request);
}