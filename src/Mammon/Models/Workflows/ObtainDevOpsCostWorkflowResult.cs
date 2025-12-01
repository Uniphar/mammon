namespace Mammon.Models.Workflows;

public record ObtainLicensesCostWorkflowResult
{
	public required ResourceCost TotalBasicLicensesCost { get; init; }
	public required ResourceCost TotalBasicPlusTestPlansLicensesCost { get; init; }
}