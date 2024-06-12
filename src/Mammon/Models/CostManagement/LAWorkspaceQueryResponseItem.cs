namespace Mammon.Models.CostManagement;

public record LAWorkspaceQueryResponseItem
{
	public required string Selector { get; set; }
	public required string SelectorType { get; set; }
	public required long SizeSum { get; set; }
}
