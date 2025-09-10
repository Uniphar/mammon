﻿namespace Mammon.Services;

public class AKSService(
	ArmClient armClient,
	LogsQueryClient logsQueryClient,
	ILogger<AKSService> logger) : BaseLogService
{
	public async Task<(IEnumerable<AKSVMSSUsageResponseItem> usageElements, bool success)> QueryUsage(string vmssResourceId, DateTime from, DateTime to)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(vmssResourceId);

        Response<IReadOnlyList<AKSVMSSUsageResponseItem>> response;

        try
		{
#if (DEBUG || INTTEST)

			string? mockApiResponsePath;
			if (!string.IsNullOrWhiteSpace(mockApiResponsePath = Consts.MockAKSResponseFilePathConfigKey)
				&& File.Exists(mockApiResponsePath))
			{
				response = await ParseMockFileAsync<AKSVMSSUsageResponseItem>(mockApiResponsePath, vmssResourceId);
			}
			else
			{
#endif
                var rID = new ResourceIdentifier(vmssResourceId);
                var rgTags = await armClient.GetResourceGroupResource(rID.GetResourceGroupIdentifier()).GetTagResource().GetAsync();

                var clusterRG = rgTags.Value.Data.TagValues["aks-managed-cluster-rg"];
                var clusterName = rgTags.Value.Data.TagValues["aks-managed-cluster-name"];

                ResourceIdentifier sID = new($"/subscriptions/{rID.SubscriptionId}");

                var rgResponse = await armClient.GetSubscriptionResource(sID).GetResourceGroups().GetAsync(clusterRG);
                var clusterResponse = await rgResponse.Value.GetContainerServiceManagedClusterAsync(clusterName);

                var laWorkspaceId = clusterResponse.Value.Data.AddonProfiles["omsagent"].Config["logAnalyticsWorkspaceResourceID"];

                var workspace = await armClient.GetOperationalInsightsWorkspaceResource(new ResourceIdentifier(laWorkspaceId)).GetAsync();

                response = await logsQueryClient.QueryWorkspaceAsync<AKSVMSSUsageResponseItem>(workspace.Value.Data.CustomerId.ToString(),
					@$"Perf
					| where ObjectName == 'K8SContainer'
						and CounterName in ('cpuUsageNanoCores', 'memoryWorkingSetBytes')
					| summarize InstanceValue=avg(CounterValue)by InstanceName, CounterName
					| extend
						PodUid = tostring(split(InstanceName, '/', 9)[0]),
						Container = tostring(split(InstanceName, '/', 10)[0])
					| join kind=leftouter (KubePodInventory
						| summarize arg_max(TimeGenerated, *) by PodUid)
						on PodUid
					| extend Nodepool = tostring(split(Computer, '-', 1)[0])
					| extend ScaleSet = strcat(tostring(split(Computer, ""-vmss"",0)[0]),""-vmss"")
					| where ScaleSet =='{rID.Name}'
					| summarize AvgInstanceValue=toreal(avg(InstanceValue)) by Namespace, CounterName",
				new QueryTimeRange(from, to));
#if (DEBUG || INTTEST)
			}
#endif

			if (response.GetRawResponse() == null || response.GetRawResponse().IsError)
			{
				return ([], false);
			}

			return (response.Value, true);		
		}
		catch (Exception e)
		{
			logger.LogError(e, $"Error querying AKS VMSS {vmssResourceId}");
			return ([], false);
		}
	}
}
