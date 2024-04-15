global using Azure.Core;
global using Azure.Identity;
global using Azure.ResourceManager;
global using Dapr.Actors;
global using Dapr.Actors.Client;
global using Dapr.Actors.Runtime;
global using FluentValidation;
global using Mammon.Actors;
global using Mammon.Extensions;
global using Mammon.Models.Actors;
global using Mammon.Models.CostManagement;
global using Mammon.Services;
global using Mammon.Utils;
global using Microsoft.ApplicationInsights;
global using Polly;
global using Polly.Extensions.Http;
global using Polly.Retry;
global using System.Data;
global using System.Diagnostics;
global using System.Net;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;

#if (DEBUG)
Debugger.Launch();
#endif

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
#if (DEBUG)
    var subActor = ActorProxy.Create<ISubscriptionActor>(new Dapr.Actors.ActorId("uniphar-dev"), "SubscriptionActor",
        new ActorProxyOptions { RequestTimeout = Timeout.InfiniteTimeSpan });

    await subActor.RunWorkload(new Mammon.Models.Actors.CostReportRequest { SubscriptionName = "uniphar-dev", CostFrom = DateTime.UtcNow.AddDays(-31), CostTo = DateTime.UtcNow.AddDays(-1) });
    //app.StopAsync().Wait();
#endif
});

app.Lifetime.ApplicationStopped.Register(() => app.Services.GetRequiredService<TelemetryClient>().FlushAsync(default).Wait());

app.Run();
