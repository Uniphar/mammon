namespace Mammon.Models.Actors;

public record LAWorkspaceActorState
{
	public string ResourceId { get; set; } = string.Empty;
	public Dictionary<string, string>? Tags { get; set; } = [];
	public ResourceCost? TotalCost { get; set; }
}
