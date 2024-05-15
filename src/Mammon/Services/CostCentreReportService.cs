namespace Mammon.Services;

public class CostCentreReportService (IServiceProvider sp, CostCentreRuleEngine costCentreRuleEngine)
{
	public async Task<string> GenerateReportAsync(string reportId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(reportId);

		var costCentres = costCentreRuleEngine.CostCentres;

		Dictionary<string, CostCentreActorState> costCentreStates = [];

		foreach (var costCentre in costCentres)
		{
			//TODO: handle actor not existing
			var state = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor, CostCentreActorState>(CostCentreActor.GetActorId(reportId, costCentre), nameof(CostCentreActor), async (p) => await p.GetCostsAsync());
			if (state != null)
			{
				costCentreStates.Add(costCentre, state);
			}
		}

		var viewModel = BuildViewModel(costCentreStates);

		return await ViewRenderer.RenderViewToStringAsync("EmailReport", viewModel, ControllerContext);
	}

	public CostCentreReportModel BuildViewModel(IDictionary<string, CostCentreActorState> costCentreStates)
	{
		CostCentreReportModel emailReportModel = new();

		foreach (var costCentre in costCentreStates)
		{
			var pivots = costCentre.Value.ResourceCosts?.Select(x => costCentreRuleEngine.ProjectCostReportPivotEntry(x.Key, x.Value));
			if (pivots != null)
			{
				var pivotGroups = pivots.GroupBy(x => (x.PivotName, x.SubscriptionId)).ToList();

				pivotGroups.Sort(new PivotDefinitionComparer());
				foreach (var pivotGroup in pivotGroups)
				{
					var pivotName = pivotGroup.Key.PivotName;

					var nodeClass = costCentreRuleEngine.ClassifyPivot(pivotGroup.First());
					var environment = costCentreRuleEngine.LookupEnvironment(pivotGroup.Key.SubscriptionId);

					emailReportModel.AddLeaf(costCentre.Key, pivotName, environment, pivotGroup.Sum(x => x.Cost), nodeClass);
				}
			}
		}

		return emailReportModel;
	}

	private ControllerContext ControllerContext
	{
		get {
			var httpContext = new DefaultHttpContext();

			var currentUri = new Uri("http://localhost"); //we will not generate any external links
			httpContext.Request.Scheme = currentUri.Scheme;
			httpContext.Request.Host = HostString.FromUriComponent(currentUri);
			httpContext.RequestServices = sp;
			var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
			var ctx = new ControllerContext(actionContext);

			return ctx;
		}
	}

	private class PivotDefinitionComparer : IComparer<IGrouping<(string pivotName,string subId), CostReportPivotEntry>>
	{
		public int Compare(IGrouping<(string pivotName, string subId), CostReportPivotEntry>? x, IGrouping<(string pivotName, string subId), CostReportPivotEntry>? y)
		{
			if (x == null && y == null)
				return 0;
			else if (x == null)
				return -1;
			else if (y == null)
				return 1;
			else
				return string.CompareOrdinal(x.Key.pivotName, y.Key.pivotName);
		}
	}
}
