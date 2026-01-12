namespace Mammon.Models.Actors;

public record CostCentreActorState
{
    public ResourceCost? TotalCost { get; set; }
    public Dictionary<string, ResourceCost> ResourceCosts { get; set; } = new();
    //TODO: We may consider the option to split this. 
    public Dictionary<string, Dictionary<string, ResourceCost>>? DevOpsProjectCosts { get; set; }
    public Dictionary<string, ResourceCost>? VisualStudioSubscriptionsCosts { get; set; }
}