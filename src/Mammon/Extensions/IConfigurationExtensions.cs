namespace Mammon.Extensions;

public static class IConfigurationExtensions
{
	public static string? GetWorkloadIdentityName(this IConfiguration configuration)
	{
		return configuration.GetValue<string>(Consts.ConfigKeyWorkloadIdentityServiceAccountName);
	}
}
