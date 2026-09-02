namespace Mammon.Services;

/// <summary>
/// Replaces the old dapr bindings.cron component (route "api/mammon/cron") which is no longer available in catalyst.
/// Periodically checks whether the configured monthly billing period boundary has been reached and, if so,
/// publishes the default cost report request exactly as the old cron-triggered endpoint used to do.
/// </summary>
public class CostReportCronBackgroundService(
    DaprClient daprClient,
    CostCentreReportService costCentreReportService,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<CostReportCronBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    private DateOnly? lastTriggeredDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                await RunIfDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed while evaluating/publishing the scheduled cost report.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RunIfDueAsync(CancellationToken cancellationToken)
    {
        var billingPeriodStartDay = int.Parse(configuration[Consts.ReportBillingPeriodStartDayInMonthConfigKey]!, CultureInfo.InvariantCulture);

        var now = timeProvider.GetLocalNow();
        var today = DateOnly.FromDateTime(now.DateTime);

        // mirrors the old "0 0 1 <day> * *" cron schedule (01:00 on the configured day of month),
        // falling back to the last day of the month if it is shorter than the configured day.
        var effectiveDay = Math.Min(billingPeriodStartDay, DateTime.DaysInMonth(today.Year, today.Month));
        var isDue = today.Day == effectiveDay && now.Hour >= 1;

        // lastTriggeredDate guards against firing again on every subsequent 30-minute check
        // for the rest of the same due day (e.g. 01:00 and 01:30 must not both publish).
        if (isDue && lastTriggeredDate != today)
        {
            await daprClient.PublishEventAsync(
                Consts.MammonPubSubCRDName,
                Consts.MammonServiceBusTopicName,
                costCentreReportService.GenerateDefaultReportRequest(),
                cancellationToken);

            lastTriggeredDate = today;
            logger.LogInformation("Published scheduled cost report request for {Date}.", today);
        }
    }
}
