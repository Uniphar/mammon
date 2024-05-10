namespace Mammon.Services;

public class CostReportService (IServiceProvider sp, CostCentreRuleEngine costCentreRuleEngine)
{
	public async Task<string> GenerateReport(string reportId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(reportId);

		var costCentres = costCentreRuleEngine.CostCentres;

		EmailReportModel emailReportModel = new();

		foreach (var costCentre in costCentres)
		{
			//TODO: handle actor not existing
			var state = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor, CostCentreActorState>(CostCentreActor.GetActorId(reportId, costCentre), nameof(CostCentreActor), async (p) => await p.GetCostsAsync());
			if (state?.ResourceCosts != null)
			{
				//group by resource group (with any configured in tokens removed)
				var resourceGroupCosts = state.ResourceCosts.GroupBy(x => costCentreRuleEngine.ProcessResourceGroupName(x.Key));
				foreach (var resourceGroupCost in resourceGroupCosts)
				{					
					emailReportModel.AddLeaf(resourceGroupCost.Key, resourceGroupCost.Sum(x=>x.Value), costCentreRuleEngine.ClassifyResourceGroup(resourceGroupCost.Key));					
				}
			}
		}
		
		return await ViewRenderer.RenderViewToStringAsync("EmailReport", new EmailReportModel(), ControllerContext);
	}

	private ControllerContext ControllerContext
	{
		get {
			var httpContext = new DefaultHttpContext();

			var currentUri = new Uri("http://localhost"); // Here you have to set your url if you want to use links in your email
			httpContext.Request.Scheme = currentUri.Scheme;
			httpContext.Request.Host = HostString.FromUriComponent(currentUri);
			httpContext.RequestServices = sp;
			var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
			var ctx = new ControllerContext(actionContext);

			return ctx;
		}
	}
}
