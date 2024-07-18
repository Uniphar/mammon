namespace Mammon.Models.CostManagement;

public record SpecialModeDefinition
{
	public required string Name { get; set; }
	public IList<string> ResourceGroupFilter { get; set; } = [];
}
