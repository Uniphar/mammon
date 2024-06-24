namespace Mammon.Models.Workflows.Activities;

public record AKSVMSSObtainUsageDataActivityRequest
{
	public required string VMSSResourceId { get; set; }
	public required DateTime FromDateTime { get; set; }
	public required DateTime ToDateTime { get; set; }
}
