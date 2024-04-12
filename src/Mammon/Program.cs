using Azure.Identity;
using Azure.ResourceManager;
using Dapr.Actors.Client;
using Mammon.Actors;
using MammonActors.Actors;
using MammonActors.Extensions;
using MammonActors.Services;
using MammonActors.Utils;
using Microsoft.ApplicationInsights;
using Polly.Extensions.Http;
using System.Data;
using System.Diagnostics;

Debugger.Launch();

var builder = WebApplication.CreateBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new NoNullAllowedException("ASPNETCORE_ENVIRONMENT environment variable is not set.");

if (environment != "local")
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://uni-devops-app-{environment}-kv.vault.azure.net/"),
        new DefaultAzureCredential());
}

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddActors(options =>
{
    // Register actor types and configure actor settings
    options.Actors.RegisterActor<ResourceActor>();
    options.Actors.RegisterActor<SubscriptionActor>();
    options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig()
    {
        Enabled = true, //TODO: do I really want to enable this?
        MaxStackDepth = 32,
    };
});

builder.Services
    .AddTransient((sp) => new ArmClient(new DefaultAzureCredential()))
    .AddTransient<DefaultAzureCredential>()
    .AddTransient<AzureAuthHandler>();

var policy = HttpPolicyExtensions
    .HandleTransientHttpError() // HttpRequestException, 5XX and 408
    .OrResult(response => (int)response.StatusCode == 429) // RetryAfter
    .AddCostManagementRetryPolicy();

builder.Services
    .AddHttpClient<CostManagementService>()
    .AddHttpMessageHandler<AzureAuthHandler>()
    .AddPolicyHandler(policy);

var app = builder.Build();

app.MapActorsHandlers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    var subActor = ActorProxy.Create<ISubscriptionActor>(new Dapr.Actors.ActorId("uniphar-dev"), "SubscriptionActor"
#if (DEBUG)
        , new ActorProxyOptions { RequestTimeout = Timeout.InfiniteTimeSpan }
#endif
        );
#if (DEBUG)
    await subActor.RunWorkload(new Mammon.Models.Actors.CostReportRequest { SubscriptionName = "uniphar-dev", costFrom = DateTime.UtcNow.AddDays(-31), costTo = DateTime.UtcNow.AddDays(-1) });
    app.StopAsync().Wait();
#endif
});

app.Lifetime.ApplicationStopped.Register(() => app.Services.GetRequiredService<TelemetryClient>().FlushAsync(default).Wait());

app.Run();
