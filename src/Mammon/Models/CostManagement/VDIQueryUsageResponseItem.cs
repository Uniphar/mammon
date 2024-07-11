namespace Mammon.Models.CostManagement;

public record VDIQueryUsageResponseItem
{
	public required string GroupID { get; set; }
	public required long SessionCount { get; set; }
}
