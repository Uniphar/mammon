global using Azure;
global using Azure.Core;
global using Azure.Identity;
global using Azure.ResourceManager;
global using Azure.ResourceManager.Resources;
global using Dapr;
global using Dapr.Actors;
global using Dapr.Actors.Client;
global using Dapr.Actors.Runtime;
global using Dapr.Client;
global using Dapr.Workflow;
global using FluentValidation;
global using Mammon.Actors;
global using Mammon.Extensions;
global using Mammon.Models.Actors;
global using Mammon.Models.CostManagement;
global using Mammon.Models.Workflows;
global using Mammon.Models.Workflows.Activities;
global using Mammon.Services;
global using Mammon.Utils;
global using Mammon.Workflows;
global using Mammon.Workflows.Activities;
global using Microsoft.ApplicationInsights;
global using Microsoft.AspNetCore.Mvc;
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

var configKVURL = builder.Configuration["CONFIG_KEYVAULT_URL"]?.ToString();
if (string.IsNullOrWhiteSpace(configKVURL))
    throw new InvalidOperationException("CONFIG_KEYVAULT_URL environment variable is not set");

builder.Configuration.AddAzureKeyVault(
    new Uri(configKVURL),
    new DefaultAzureCredential());

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddControllers();

builder.Services
    .AddDaprWorkflow((config) => { 
        config.RegisterWorkflow<SubscriptionWorkflow>();
        config.RegisterActivity<ObtainCostsActivity>();
        config.RegisterActivity<CallResourceActorActivity>();
    })
    .AddActors(options => {
        // Register actor types and configure actor settings
        options.Actors.RegisterActor<ResourceActor>();   
        options.ReentrancyConfig = new ActorReentrancyConfig()
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

app.UseRouting();

app.MapActorsHandlers();

app.MapControllers();

app.MapSubscribeHandler();

app.Lifetime.ApplicationStopped.Register(() => app.Services.GetRequiredService<TelemetryClient>().FlushAsync(default).Wait());

app.Run();
