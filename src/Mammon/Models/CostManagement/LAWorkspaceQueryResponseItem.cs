namespace Mammon.Models.CostManagement;

public record LAWorkspaceQueryResponseItem
{
	public required string Selector { get; set; }
	[JsonIgnore]
	public ResourceIdentifier? SelectorIdentifier => SelectorType==Consts.ResourceIdLAWorkspaceSelectorType ? new(Selector): null;
	public required string SelectorType { get; set; }
	public required long SizeSum { get; set; }
}
