namespace Mammon.Actors;

public class VisualStudioSubscriptionCostActor(
    ActorHost actorHost, ILogger<VisualStudioSubscriptionCostActor> logger,
    CostCentreService costCentreService,
    CostCentreRuleEngine costCentreRuleEngine)
    : ActorBase<CoreResourceActorState>(actorHost), IVisualStudioSubscriptionCostActor
{
    public async Task SplitCostAsync(VisualStudioSubscriptionsSplittableResourceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.VisualStudioSubscriptionCosts.Count == 0)
        {
            logger.LogWarning($"No Visual Studio subscription costs to split for ReportRequestId: {request.ReportRequest.ReportId}, SubscriptionId: {request.ReportRequest.SubscriptionId}");
            return;
        }

        var costCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(request.ReportRequest.ReportId, request.ReportRequest.SubscriptionId);

        try
        {
            Dictionary<string, ResourceCost> costCentreCosts = [];

            foreach (var vs in request.VisualStudioSubscriptionCosts)
            {
                var billedTotal = vs.Cost.Cost;
                var billedCurrency = vs.Cost.Currency;

                var unitCost = ResolveUnitCost(vs.Product);

                if (!string.Equals(unitCost.Currency, billedCurrency, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Currency mismatch for '{vs.Product}': billed={billedCurrency}, unit={unitCost.Currency}");

                decimal allocatedSoFar = 0m;

                foreach (var kvp in costCentreRuleEngine.StaticVisualStudioLicensesMapping)
                {
                    var department = kvp.Key;
                    var licenseCount = kvp.Value; // double in your model

                    if (licenseCount <= 0)
                        continue;

                    var desired = unitCost.Cost * (decimal)licenseCount;

                    // Cap so we never allocate more than what was billed for that product.
                    var remaining = billedTotal - allocatedSoFar;
                    if (remaining <= 0)
                        break;

                    var allocated = desired <= remaining ? desired : remaining;

                    AddCost(costCentreCosts, department, new ResourceCost(allocated, billedCurrency));
                    allocatedSoFar += allocated;
                }

                // Remainder to default cost centre
                var remainder = billedTotal - allocatedSoFar;
                if (remainder > 0)
                    AddCost(costCentreCosts, costCentreRuleEngine.DefaultCostCentre, new ResourceCost(remainder, billedCurrency));
            }

            foreach(var costCentreCost in costCentreCosts)
            {
                await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
                    CostCentreActor.GetActorId(request.ReportRequest.ReportId, costCentreCost.Key, request.ReportRequest.SubscriptionId),
                    nameof(CostCentreActor),
                    async (p) => await p.AddVisualStudioSubscriptionCostAsync(costCentreCost.Value));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in VisualStudioSubscriptionCostActor.SplitCostAsync (ActorId:{Id})");
            throw;
        }
    }

    static void AddCost(
        Dictionary<string, ResourceCost> target,
        string costCentre,
        ResourceCost cost)
    {
        if (target.TryGetValue(costCentre, out var existing))
        {
            target[costCentre] = new ResourceCost([existing, cost]);
            return;
        }

        target[costCentre] = cost;
    }


    private ResourceCost ResolveUnitCost(string product)
    {
        return product switch
        {
            "Visual Studio Subscription - Enterprise Monthly" => new ResourceCost(costCentreRuleEngine.VisualStudioEnterpriseMonthlyLicenseCost, "EUR"),
            "Visual Studio Subscription - Enterprise Annual" => new ResourceCost(costCentreRuleEngine.VisualStudioEnterpriseAnnualLicenseCost, "EUR"),
            "Visual Studio Subscription - Professional Monthly" => new ResourceCost(costCentreRuleEngine.VisualStudioProfessionalMonthlyLicenseCost, "EUR"),
            "Visual Studio Subscription - Professional Annual" => new ResourceCost(costCentreRuleEngine.VisualStudioProfessionalAnnualLicenseCost, "EUR"),
            _ => throw new InvalidOperationException($"Unknown Visual Studio product '{product}'")
        };
    }

}
