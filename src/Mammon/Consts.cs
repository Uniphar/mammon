namespace Mammon;

public static class Consts
{
	public const string StateStoreNameConfigKey = "Mammon:StateStoreName";

	public const string MammonServiceBusTopicName = "MammonReportRequests";
	public const string MammonPubSubCRDName = "mammon-pub-sub";

	public const string ConfigKeyVaultConfigEnvironmentVariable = "MAMMON_CONFIG_KEYVAULT_URL";
	public const string ConfigKeyWorkloadIdentityServiceAccountName = "MAMMON_CONFIG_SERVICEACCOUNTNAME";
	public const string CostCentreRuleEngineFilePathConfigKey = "Mammon:CostCentreRuleEngineFilePath";

	public const string ReportBillingPeriodStartDayInMonthConfigKey = "Mammon:ReportSettings:BillingPeriodStartDayInMonth";

	public const string ReportSubjectConfigKey = "Mammon:EmailSettings:SubjectFormat";
	public const string ReportToAddressesConfigKey = "Mammon:EmailSettings:ToAddresses";
	public const string ReportFromAddressConfigKey = "Mammon:EmailSettings:FromAddress";

	public const string DotFlyerSBConnectionStringConfigKey = "Mammon:DotFlyer:ServiceBusConnectionString";

	public const string DotFlyerAttachmentsBlobStorageConnectionStringConfigKey = "Mammon:DotFlyer:AttachmentsBlobStorageConnectionString";
	public const string DotFlyerAttachmentsContainerNameConfigKey = "Mammon:DotFlyer:AttachmentsBlobStorageContainerName";

	public const string ResourceIdLAWorkspaceSelectorType = "ResourceId";
	public const string AKSCPUMetricName = "cpuUsageNanoCores";

	public const string MammonSplittablePrefix = "MammonSplittable";

#if (DEBUG)        
	public const string MockCostAPIResponseFilePathConfigKey = "Mammon:MockCostAPIResponseFilePath";
#endif
}
