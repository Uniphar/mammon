namespace Mammon.Models.Actors
{
	public record CoreResourceActorState
	{
		public string ResourceId { get; set; } = string.Empty;
		public ResourceCost TotalCost { get; set; } = new ResourceCost(0, string.Empty);
	}
}
