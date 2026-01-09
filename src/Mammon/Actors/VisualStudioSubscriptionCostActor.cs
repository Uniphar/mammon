namespace Mammon.Actors;

public class VisualStudioSubscriptionCostActor(
    ActorHost actorHost, ILogger<VisualStudioSubscriptionCostActor> logger,
    CostCentreService costCentreService,
    CostCentreRuleEngine costCentreRuleEngine)
    : ActorBase<CoreResourceActorState>(actorHost), IVisualStudioSubscriptionCostActor
{
    private readonly IReadOnlyDictionary<string, Func<string, ResourceCost>> _unitCostResolvers =
        new Dictionary<string, Func<string, ResourceCost>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Visual Studio Subscription - Enterprise Monthly"] =
            currency => new ResourceCost(costCentreRuleEngine.VisualStudioEnterpriseMonthlyLicenseCost, currency),

            ["Visual Studio Subscription - Enterprise Annual"] =
            currency => new ResourceCost(costCentreRuleEngine.VisualStudioEnterpriseAnnualLicenseCost, currency),

            ["Visual Studio Subscription - Professional Monthly"] =
            currency => new ResourceCost(costCentreRuleEngine.VisualStudioProfessionalMonthlyLicenseCost, currency),

            ["Visual Studio Subscription - Professional Annual"] =
            currency => new ResourceCost(costCentreRuleEngine.VisualStudioProfessionalAnnualLicenseCost, currency),
        };

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
            Dictionary<string, Dictionary<string, ResourceCost>> costCentreCosts = [];

            foreach (var visualStudioSubscriptionCost in request.VisualStudioSubscriptionCosts)
            {
                var billedTotal = visualStudioSubscriptionCost.Cost.Cost;
                var billedCurrency = visualStudioSubscriptionCost.Cost.Currency;

                var unitCost = ResolveUnitCost(visualStudioSubscriptionCost);

                if (!string.Equals(unitCost.Currency, billedCurrency, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Currency mismatch for '{visualStudioSubscriptionCost.Product}': billed={billedCurrency}, unit={unitCost.Currency}");

                decimal allocatedSoFar = 0m;

                foreach (var costCentreLicenseAllocation in costCentreRuleEngine.StaticVisualStudioLicensesMapping)
                {
                    var department = costCentreLicenseAllocation.Key;
                    var licenseCount = costCentreLicenseAllocation.Value;

                    if (licenseCount <= 0)
                        continue;

                    var desired = unitCost.Cost * (decimal)licenseCount;

                    var remaining = billedTotal - allocatedSoFar;
                    if (remaining <= 0)
                        break;

                    var allocated = desired <= remaining ? desired : remaining;

                    AddCost(costCentreCosts, visualStudioSubscriptionCost.Product, department, new ResourceCost(allocated, billedCurrency));
                    allocatedSoFar += allocated;
                }

                // Remainder to default cost centre
                var remainder = billedTotal - allocatedSoFar;
                if (remainder > 0)
                    AddCost(costCentreCosts, visualStudioSubscriptionCost.Product, costCentreRuleEngine.DefaultCostCentre, new ResourceCost(remainder, billedCurrency));
            }

            foreach(var costCentreCost in costCentreCosts)
            {
                foreach(var subCostCentreCost in costCentreCost.Value)
                {
                    await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(
                    CostCentreActor.GetActorId(request.ReportRequest.ReportId, costCentreCost.Key, request.ReportRequest.SubscriptionId),
                    nameof(CostCentreActor),
                    async (p) => await p.AddVisualStudioSubscriptionCostAsync(subCostCentreCost.Key, subCostCentreCost.Value));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failure in VisualStudioSubscriptionCostActor.SplitCostAsync (ActorId:{Id})");
            throw;
        }
    }

    private static void AddCost(
        Dictionary<string, Dictionary<string, ResourceCost>> target,
        string product,
        string costCentre,
        ResourceCost cost)
    {
        if (target.TryGetValue(costCentre, out var existing))
        {
            if (existing.TryGetValue(product, out var existingCost))
            {
                existing[product] = new ResourceCost(existingCost.Cost + cost.Cost, cost.Currency);
            }
            else
            {
                existing[product] = cost;
            }
        }
        else
        {
            target[costCentre] = new Dictionary<string, ResourceCost>
            {
                {  product, cost }
            };
        }
    }

    private ResourceCost ResolveUnitCost(VisualStudioSubscriptionCostResponse vsSubCost)
    {
        if (_unitCostResolvers.TryGetValue(vsSubCost.Product, out var resolver))
        {
            return resolver(vsSubCost.Cost.Currency);
        }

        throw new InvalidOperationException(
            $"Unknown Visual Studio product '{vsSubCost.Product}'");
    }
}
