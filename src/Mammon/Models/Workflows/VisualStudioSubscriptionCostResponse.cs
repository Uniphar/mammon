namespace Mammon.Models.Workflows;

public record VisualStudioSubscriptionCostResponse
{
    public ResourceCost Cost { get; set; } = default!;
    public string Product { get; set; } = string.Empty;
}