namespace Mammon.Tests.Workflow;

[TestClass, TestCategory("IntegrationTest")]
public class TenantWorkflowTests
{
	private static SecretClient? kvClient;
	private static ServiceBusClient? _serviceBusClient;
	private static ServiceBusSender? _serviceBusSender;
	private static ICslQueryProvider? _cslQueryProvider;
	private static string? _reportSubject = string.Empty;

	private static CancellationToken _cancellationToken;

	[ClassInitialize]
	public static async Task Initialize(TestContext testContext)
	{
		_cancellationToken = testContext.CancellationTokenSource.Token;

		var kvUrl = Environment.GetEnvironmentVariable(Mammon.Consts.ConfigKeyVaultConfigEnvironmentVariable);
		ArgumentException.ThrowIfNullOrWhiteSpace(kvUrl, nameof(kvUrl));

		DefaultAzureCredential azureCredential = new();

		ConfigurationBuilder configurationBuilder = new();
		configurationBuilder.AddAzureKeyVault(new Uri(kvUrl), azureCredential);
		var config = configurationBuilder.Build();

		kvClient = new(new Uri(kvUrl), azureCredential);

		var sbConnectionString = config[Mammon.Consts.DotFlyerSBConnectionStringConfigKey];

		_reportSubject = config[Mammon.Consts.ReportSubjectConfigKey];

		_serviceBusClient = new(sbConnectionString, azureCredential);
		_serviceBusSender = _serviceBusClient.CreateSender(Mammon.Consts.MammonServiceBusTopicName);

		var _adxHostAddress = config["AzureDataExplorer:HostAddress"];

		var kcsb = new KustoConnectionStringBuilder(_adxHostAddress, "devops")
			.WithAadTokenProviderAuthentication(async () =>
				(await azureCredential.GetTokenAsync(new(["https://kusto.kusto.windows.net/.default"]), cancellationToken: _cancellationToken)).Token);

		_cslQueryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
	}

	[TestMethod]
	public async Task WorkflowFinishesAndSendsEmailAsync()
	{
		//send report request to SB Topic to wake up Mammon
		var reportId = Guid.NewGuid().ToString();

		await _serviceBusSender!.SendMessageAsync(new ServiceBusMessage
		{
			Body = BinaryData.FromObjectAsJson(new 
			{
				data = new CostReportRequest 
					{
						ReportId = reportId, 
						CostFrom = DateTime.UtcNow.AddDays(-1), 
						CostTo = DateTime.UtcNow 
					}
				}),
			ContentType = "application/json",
		});

		//wait for ADX to record email produced
		string expectedSubject = string.Format(_reportSubject!, reportId);
		EmailData emailData = await _cslQueryProvider!.WaitSingleQueryResult<EmailData>($"DotFlyerEmails | where Subject == \"{expectedSubject}\"", TimeSpan.FromMinutes(30), _cancellationToken);
		emailData.Should().NotBeNull();
		//emailData.Attachments.Should().NotBeEmpty();//TODO: this needs fix in the common model
	}

	public class EmailData : EmailMessage
	{
		public required string FromEmail { get; set; }

		public required string FromName { get; set; }

		public new required string To { get; set; }

		public new required string Cc { get; set; }

		public new required string Attachments { get; set; }

		public new required string Tags { get; set; }

		public required int SendGridStatusCodeInt { get; set; }

		public required string SendGridStatusCodeString { get; set; }

		public required string SendGridResponseContent { get; set; }

		public required DateTime IngestDateTimeUtc { get; set; }

		public class Attachment(string URI)
		{
			public string URI { get; } = URI;
		}
	}
}
