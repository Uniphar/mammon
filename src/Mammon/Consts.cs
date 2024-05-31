namespace Mammon;

public static class Consts
{
	public const string StateStoreNameConfigKey = "Mammon:StateStoreName";

	public const string ConfigKeyVaultConfigEnvironmentVariable = "MAMMON_CONFIG_KEYVAULT_URL";
	public const string MockCostAPIResponseFilePathConfigKey = "Mammon:MockCostAPIResponseFilePath";
	public const string CostCentreRuleEngineFilePathConfigKey = "Mammon:CostCentreRuleEngineFilePath";

	public const string ReportSubjectConfigKey = "Mammon:EmailSettings:SubjectFormat";
	public const string ReportToAddressesConfigKey = "Mammon:EmailSettings:ToAddresses";
	public const string ReportFromAddressConfigKey = "Mammon:EmailSettings:FromAddress";

	public const string DotFlyerSBConnectionStringConfigKey = "Mammon:DotFlyer:ServiceBusConnectionString";

	public const string DotFlyerAttachmentsBlobStorageConnectionStringConfigKey = "Mammon:DotFlyer:AttachmentsBlobStorageConnectionString";
	public const string DotFlyerAttachmentsContainerNameConfigKey = "Mammon:DotFlyer:AttachmentsBlobStorageContainerName";
}
