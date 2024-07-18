namespace Mammon.Actors;

public class AKSVMSSActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine, ILogger<AKSVMSSActor> logger) : ActorBase<CoreResourceActorState>(host), IAKSVMSSActor
{
	private const string CostStateName = "AKSVMSSActorState";

	public async Task SplitCost(SplittableResourceRequest request, IEnumerable<AKSVMSSUsageResponseItem> data)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(request);

			var totalCost = request.Resource.Cost;
			var resourceId = request.Resource.ResourceId;
			var reportId = request.ReportRequest.ReportId;

			Dictionary<string, NamespaceMetrics> nsMetrics = [];

			var state = await GetStateAsync(CostStateName);

			state.TotalCost = totalCost;

			await SaveStateAsync(CostStateName, state);

			foreach (var item in data)
			{
				var costCentre = costCentreRuleEngine.GetCostCentreForAKSNamespace(item.Namespace);

				if (!nsMetrics.TryGetValue(costCentre, out NamespaceMetrics? value))
				{
					value = new NamespaceMetrics();

					nsMetrics.Add(costCentre, value);
				}

				if (item.CounterName == Consts.AKSCPUMetricName)
					value.CPUMetricValue += item.AvgInstanceValue/10e9; //normalize to full core
				else
					value.MemMetricValue += item.AvgInstanceValue/10e9; //normalize to gigabytes
			}

			var totalScore = nsMetrics.Values.Sum(x => x.Score);

			foreach (var nsMetric in nsMetrics)
			{
				var cost = new ResourceCost((decimal)(nsMetric.Value.Score / totalScore) * totalCost.Cost, totalCost.Currency);
				await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, nsMetric.Key), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, cost));
			}

			//assumption here of at least one namespace ("default") with usage so no unallocated cost should exist
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Failure in AKSVMSSActor.SplitCost (ActorId:{Id})");
			throw;
		}
	}

	internal record NamespaceMetrics 
	{
		internal double CPUMetricValue { get; set; }
		internal double MemMetricValue { get; set; }

		internal double Score=> 2*CPUMetricValue + MemMetricValue;
	}
}
