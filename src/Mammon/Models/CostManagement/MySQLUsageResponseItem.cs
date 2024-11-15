namespace Mammon.Models.CostManagement;

public record MySQLUsageResponseItem
{
	public required string DBName;
	public required long DBSize;
}
