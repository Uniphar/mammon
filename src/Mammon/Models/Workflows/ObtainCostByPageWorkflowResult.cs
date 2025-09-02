namespace Mammon.Models.Workflows;

public record ObtainCostByPageWorkflowResult
{
	public required ResourceCostResponse[] Costs { get; init; }
	public required bool NextPageAvailable { get; init; }
}
