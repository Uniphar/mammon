namespace Mammon.Models.Workflows.Activities.MySQL;

public record MySQLServerSplitUsageActivityRequest
{
	public required string ResourceId { get; set; }
	public required string ReportId { get; set; }
}
