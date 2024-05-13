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
			var resources = costCentre.Value.ResourceCosts;
			if (resources != null)
			{
				var resourceGroupCosts = resources.GroupBy(x => costCentreRuleEngine.ProcessResourceGroupName(x.Key)).ToList();

				resourceGroupCosts.Sort(new ResourceGroupComparer());
				foreach (var resourceGroupCost in resourceGroupCosts)
				{
					var rgName = resourceGroupCost.Key.parsedOutName;

					var nodeClass = costCentreRuleEngine.ClassifyResourceGroup(rgName);
					var environment = costCentreRuleEngine.LookupEnvironment(resourceGroupCost.Key.subcriptionId);

					emailReportModel.AddLeaf(costCentre.Key, rgName, environment, resourceGroupCost.Sum(x => x.Value), nodeClass);
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

	private class ResourceGroupComparer : IComparer<IGrouping<(string rgName, string subId), KeyValuePair<string, double>>>
	{
		public int Compare(IGrouping<(string rgName, string subId), KeyValuePair<string, double>>? x, IGrouping<(string rgName, string subId), KeyValuePair<string, double>>? y)
		{
			if (x == null && y == null)
				return 0;
			else if (x == null)
				return -1;
			else if (y == null)
				return 1;
			else
				return string.CompareOrdinal(x.Key.rgName, y.Key.rgName);
		}
	}
}
