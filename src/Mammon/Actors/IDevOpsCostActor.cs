namespace Mammon.Actors;

public interface IDevOpsCostActor : IActor
{
	Task SplitCost(DevopsResourceRequest request);
}