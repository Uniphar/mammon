namespace Mammon.Actors;

public class AKSVMSSActor(ActorHost host, CostCentreRuleEngine costCentreRuleEngine) : Actor(host), IAKSVMSSActor
{
	public static string GetActorId(string reportId, string vmssName, string subId) => $"{reportId}_{subId}_{vmssName}";

	public async Task SplitCost(string reportId, string resourceId, ResourceCost vmssTotalCost, IEnumerable<AKSVMSSUsageResponseItem> data)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
		ArgumentException.ThrowIfNullOrWhiteSpace(reportId);
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(vmssTotalCost);

		Dictionary<string, NamespaceMetrics> nsMetrics = [];
		foreach (var item in data)
		{
			var costCentre = costCentreRuleEngine.GetCostCentreForAKSNamespace(item.Namespace);

			if (!nsMetrics.TryGetValue(costCentre, out NamespaceMetrics? value))
			{
				value = new NamespaceMetrics();
								
				nsMetrics.Add(costCentre, value);
			}

			if (item.CounterName == Consts.AKSCPUMetricName)
				value.CPUMetricValue += item.AvgInstanceValue;
			else
				value.MemMetricValue += item.AvgInstanceValue;			
		}

		var totalScore = nsMetrics.Values.Sum(x => x.Score);

		foreach (var nsMetric in nsMetrics)
		{
			var cost = new ResourceCost((decimal)(nsMetric.Value.Score / totalScore) * vmssTotalCost.Cost, vmssTotalCost.Currency);
			await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor>(CostCentreActor.GetActorId(reportId, nsMetric.Key), nameof(CostCentreActor), async (p) => await p.AddCostAsync(resourceId, cost));
		}
	}

	internal record NamespaceMetrics 
	{
		internal double CPUMetricValue { get; set; }
		internal double MemMetricValue { get; set; }

		internal double Score=> 2*CPUMetricValue + MemMetricValue;
	}
}
