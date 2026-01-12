namespace Mammon;

public static class Consts
{
	public const string StateStoreNameConfigKey = "Mammon:StateStoreName";

	public const string MammonServiceBusTopicName = "MammonReportRequests";
	public const string MammonPubSubCRDName = "mammon-pub-sub";

	public const string ConfigKeyVaultConfigEnvironmentVariable = "MAMMON_CONFIG_KEYVAULT_URL";
	public const string CostCentreRuleEngineFilePathConfigKey = "Mammon:CostCentreRuleEngineFilePath";
	public const string CostCentreRuleEngineDevOpsConfigKey = "Mammon:CostCentreRuleEngineDevOpsConfigFilePath";
	public const string AzureDevOpsPATConfigKey = "Mammon:AzureDevOps:PAT";

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

#if (DEBUG || INTTEST)
	public const string MockCostAPIResponseFilePathConfigKey = "Mammon:MockCostAPIResponseFilePath";
	public const string MockDevOpsCostAPIResponseFilePathConfigKey = "Mammon:MockDevopsCostAPIResponseFilePath";
	public const string MockVisualStudioSubscriptionsCostsApiResponseFilePathConfigKey = "Mammon:MockVisualStudioSubscriptionsCostsApiResponseFilePath";
    public const string MockLAQueryResponseFilePathConfigKey = "./Services/dummyLAQueryApiResponse.json";
	public const string MockDevOpsMemberEntitlementsFilePathConfigKey = "./Services/dummyMemberEntitlementsApiResponse.json";
	public const string MockAKSResponseFilePathConfigKey = "./Services/dummyAKSQueryApiResponse.json";
	public const string MockSqlPoolResponseFilePathConfigKey = "./Services/dummySqlPoolQueryApiResponse.json";
	public const string MockVDIResponseFilePathConfigKey = "./Services/dummyVDIQueryApiResponse.json";
#endif
}
