
namespace Mammon.Workflows.Activities.MySQL
{
	public class MySQLServerSplitUsageActivity : WorkflowActivity<SplitUsageActivityRequest<MySQLUsageResponseItem>, bool>
	{
		public override async Task<bool> RunAsync(WorkflowActivityContext context, SplitUsageActivityRequest<MySQLUsageResponseItem> input)
		{
			ResourceIdentifier rId = new(input.Request.Resource.ResourceId);

			await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<IMySQLServerActor>(MySQLServerActor.GetActorId(input.Request.ReportRequest.ReportId, rId.Name, rId.SubscriptionId!), nameof(MySQLServerActor),
				async (p) => await p.SplitCost(input.Request, input.Data));


			return true;
		}
	}
}
