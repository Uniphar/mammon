namespace Mammon;

public static class Consts
{
    public const string StateStoreName = "statestore";

    public const string ConfigKeyVaultConfigEnvironmentVariable  = "CONFIG_KEYVAULT_URL";
    public const string MockCostAPIResponseFilePathConfigKey = "Mammon:MockCostAPIResponseFilePath";
    public const string CostCentreRuleEngineFilePathConfigKey = "Mammon:CostCentreRuleEngineFilePath";

    public const string ReportSubjectConfigKey = "Mammon:EmailSettings:SubjectFormat";
	public const string ReportToAddressesConfigKey = "Mammon:EmailSettings:ToAddresses";
	public const string ReportFromAddressConfigKey = "Mammon:EmailSettings:FromAddress";

    public const string ReportBlobStorageConnectionStringConfigKey = "Mammon:BlobStorage:ConnectionString";
	public const string DotFlyerSBConnectionStringConfigKey = "Mammon:DotFlyer:ServiceBusConnectionString";
}
