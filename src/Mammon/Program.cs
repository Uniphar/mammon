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

        config.RegisterActivity<ObtainCostsActivity>();
        config.RegisterActivity<CallResourceActorActivity>();
        config.RegisterActivity<AssignCostCentreActivity>();
        config.RegisterActivity<SendReportViaEmail>();
        config.RegisterActivity<ExecuteLAWorkspaceDataQueryActivity>();
        config.RegisterActivity<IdentityLAWorkspaceRefGapsActivity>();
        config.RegisterActivity<SplitLAWorkspaceCostsActivity>();
    })
    .AddActors(options => {
        options.Actors.RegisterActor<ResourceActor>();
        options.Actors.RegisterActor<CostCentreActor>();
        options.Actors.RegisterActor<LAWorkspaceActor>();
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
    .AddSingleton((sp) => TimeProvider.System)
    .AddAzureClients(clientBuilder =>
    {
        var blobServiceConnectionString = builder.Configuration[Consts.DotFlyerAttachmentsBlobStorageConnectionStringConfigKey] 
            ?? throw new InvalidOperationException("DotFlyer Blob Storage connection string is invalid");

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
