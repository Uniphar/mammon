namespace Mammon.Actors;

public interface IDevOpsCostActor : IActor
{
	Task SplitCostAsync(DevopsResourceRequest request);
}