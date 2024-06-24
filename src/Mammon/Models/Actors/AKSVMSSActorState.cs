namespace Mammon.Models.Actors;

public record AKSVMSSActorState
{
	public string ResourceId { get; set; } = string.Empty;
	public ResourceCost? TotalCost { get; set; }
}
