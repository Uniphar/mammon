namespace Mammon.Models.CostManagement;

public record AKSVMSSUsageResponseItem
{
	public required double AvgInstanceValue { get; set; }
	public required string Namespace { get; set; }
	public required string CounterName { get; set; }
}
