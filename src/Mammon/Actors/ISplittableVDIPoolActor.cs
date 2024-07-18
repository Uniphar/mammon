namespace Mammon.Actors
{
	public interface ISplittableVDIPoolActor: IActor
	{
		Task<bool> SplitCost(SplittableResourceGroupRequest request, IEnumerable<VDIQueryUsageResponseItem> data);
	}
}
