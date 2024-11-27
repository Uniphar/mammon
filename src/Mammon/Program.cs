global using Azure;
global using Azure.Core;
global using Azure.Identity;
global using Azure.Messaging.ServiceBus;
global using Azure.Monitor.Query;
global using Azure.ResourceManager;
global using Azure.ResourceManager.ContainerService;
global using Azure.ResourceManager.DesktopVirtualization;
global using Azure.ResourceManager.Monitor;
global using Azure.ResourceManager.OperationalInsights;
global using Azure.ResourceManager.Resources;
global using Azure.ResourceManager.Sql;
global using Azure.Storage.Blobs;
global using CsvHelper;
global using CsvHelper.Configuration;
global using Dapr;
global using Dapr.Actors;
global using Dapr.Actors.Client;
global using Dapr.Actors.Runtime;
global using Dapr.Client;
global using Dapr.Workflow;
global using DotFlyer.Common.Payload;
global using FluentValidation;
global using Grpc.Core;
global using Mammon;
global using Mammon.Actors;
global using Mammon.Extensions;
global using Mammon.Models.Actors;
global using Mammon.Models.CostManagement;
global using Mammon.Models.Views;
global using Mammon.Models.Workflows;
global using Mammon.Models.Workflows.Activities;
global using Mammon.Models.Workflows.Activities.LogAnalytics;
global using Mammon.Services;
global using Mammon.Utils;
global using Mammon.Workflows;
global using Mammon.Workflows.Activities;
global using Mammon.Workflows.Activities.AKS;
global using Mammon.Workflows.Activities.LogAnalytics;
global using Mammon.Workflows.Activities.MySQL;
global using Mammon.Workflows.Activities.SQLPool;
global using Mammon.Workflows.Activities.VDI;
global using Mammon.Workflows.AKS;
global using Mammon.Workflows.LogAnalytics;
global using Mammon.Workflows.MySQL;
global using Mammon.Workflows.SQLPool;
global using Mammon.Workflows.VDI;
global using Microsoft.ApplicationInsights;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.Extensions.Azure;
global using Polly;
global using Polly.Extensions.Http;
global using Polly.Retry;
global using System.Data;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Net;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using Westwind.AspNetCore.Views;

#if (DEBUG)
Debugger.Launch();
#endif

DefaultAzureCredential defaultAzureCredentials = new();

var builder = WebApplication.CreateBuilder(args);

var configKVURL = builder.Configuration[Consts.ConfigKeyVaultConfigEnvironmentVariable]?.ToString();
if (string.IsNullOrWhiteSpace(configKVURL))
	throw new InvalidOperationException($"{Consts.ConfigKeyVaultConfigEnvironmentVariable} environment variable is not set");

builder.Configuration.AddAzureKeyVault(
	new Uri(configKVURL),
	defaultAzureCredentials);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddRazorPages();

builder.Services.AddControllers();

builder.Services
    .AddDaprWorkflow((config) => { 
        config.RegisterWorkflow<SubscriptionWorkflow>();
        config.RegisterWorkflow<GroupSubWorkflow>();
        config.RegisterWorkflow<TenantWorkflow>();
        config.RegisterWorkflow<LAWorkspaceWorkflow>();
        config.RegisterWorkflow<AKSVMSSWorkflow>();
        config.RegisterWorkflow<SQLPoolWorkflow>();
        config.RegisterWorkflow<VDIWorkflow>();
        config.RegisterWorkflow<ObtainCostByPageWorkflow>();
        config.RegisterWorkflow<MySQLWorkflow>();

        config.RegisterActivity<ObtainCostsActivity>();
        config.RegisterActivity<CallResourceActorActivity>();
        config.RegisterActivity<AssignCostCentreActivity>();
        config.RegisterActivity<SendReportViaEmail>();
        config.RegisterActivity<ExecuteLAWorkspaceDataQueryActivity>();
        config.RegisterActivity<IdenfityLAWorkspaceRefGapsActivity>();
        config.RegisterActivity<SplitLAWorkspaceCostsActivity>();
        config.RegisterActivity<AKSVMSSObtainUsageDataActivity>();
        config.RegisterActivity<AKSSplitUsageCostActivity>();
        config.RegisterActivity<SQLPoolObtainUsageDataActivity>();
        config.RegisterActivity<SQLPoolSplitUsageActivity>();
        config.RegisterActivity<VDIGroupSplitObtainUsageActivity>();
        config.RegisterActivity<VDIGroupSplitUsageActivity>();
        config.RegisterActivity<MySQLServerSplitUsageActivity>();

    })
    .AddActors(options => {
        options.Actors.RegisterActor<ResourceActor>();
        options.Actors.RegisterActor<CostCentreActor>();
        options.Actors.RegisterActor<LAWorkspaceActor>();
        options.Actors.RegisterActor<AKSVMSSActor>();
        options.Actors.RegisterActor<SQLPoolActor>();
        options.Actors.RegisterActor<SplittableVDIPoolActor>();
        options.Actors.RegisterActor<MySQLServerActor>();

        options.ReentrancyConfig = new ActorReentrancyConfig()
        {
            Enabled = false
        };
    });

builder.Services
    .AddTransient((sp) => new ArmClient(defaultAzureCredentials))
    .AddTransient<AzureAuthHandler>()
    .AddSingleton(defaultAzureCredentials)
    .AddSingleton<CostCentreRuleEngine>()
    .AddSingleton<CostCentreReportService>()
    .AddSingleton<CostCentreService>()
    .AddSingleton<LogAnalyticsService>()
    .AddSingleton<AKSService>()
    .AddSingleton<SQLPoolService>()
    .AddSingleton<VDIService>()
    .AddSingleton((sp) => TimeProvider.System)
    .AddAzureClients(clientBuilder =>
    {
        var blobServiceConnectionString = builder.Configuration[Consts.DotFlyerAttachmentsBlobStorageConnectionStringConfigKey] 
            ?? throw new InvalidOperationException("DotFlyer Blob Storage connection string is invalid");

        clientBuilder.AddLogsQueryClient();
		clientBuilder.AddBlobServiceClient(new Uri(blobServiceConnectionString));
        clientBuilder.AddServiceBusClientWithNamespace(builder.Configuration[Consts.DotFlyerSBConnectionStringConfigKey] ?? throw new InvalidOperationException("DotFlyer SB connection string is invalid"));
        clientBuilder.UseCredential(defaultAzureCredentials);
    });


var policy = HttpPolicyExtensions
    .HandleTransientHttpError() // HttpRequestException, 5XX and 408
    .OrResult(response => (int)response.StatusCode == 429) // RetryAfter
    .AddCostManagementRetryPolicy();

builder.Services
    .AddHttpClient<CostRetrievalService>()
    .AddHttpMessageHandler<AzureAuthHandler>()
    .AddPolicyHandler(policy);

var app = builder.Build();

CostCentreReportService.ValidateConfiguration(app.Configuration);

app.UseRouting();

app.MapActorsHandlers();

app.MapRazorPages();

app.MapControllers();

app.MapSubscribeHandler();

app.Lifetime.ApplicationStopped.Register(() => app.Services.GetRequiredService<TelemetryClient>().FlushAsync(default).Wait());

app.Run();
