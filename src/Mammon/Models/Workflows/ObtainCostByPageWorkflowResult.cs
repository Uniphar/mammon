namespace Mammon.Models.Workflows;

public record ObtainCostByPageWorkflowResult
{
	public required ResourceCostResponse[] Costs { get; set; }
	public required bool nextPageAvailable { get; set; }
}
