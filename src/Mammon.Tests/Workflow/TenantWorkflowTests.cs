namespace Mammon.Tests.Workflow;

[TestClass, TestCategory("IntegrationTest")]
public class TenantWorkflowTests
{
	private static ServiceBusClient? _serviceBusClient;
	private static ServiceBusSender? _serviceBusSender;
	private static ICslQueryProvider? _cslQueryProvider;
	private static string? _reportSubject = string.Empty;
	private static string? _fromEmail = string.Empty;
	private static string? _toEmail = string.Empty;

	private static CancellationToken _cancellationToken;

	[ClassInitialize]
	public static void Initialize(TestContext testContext)
	{
		_cancellationToken = testContext.CancellationTokenSource.Token;

		var kvUrl = Environment.GetEnvironmentVariable(Mammon.Consts.ConfigKeyVaultConfigEnvironmentVariable);
		ArgumentException.ThrowIfNullOrWhiteSpace(kvUrl, nameof(kvUrl));

		DefaultAzureCredential azureCredential = new();

		ConfigurationBuilder configurationBuilder = new();
		configurationBuilder.AddAzureKeyVault(new Uri(kvUrl), azureCredential);
		var config = configurationBuilder.Build();

		var sbConnectionString = config[Consts.DotFlyerSBConnectionStringConfigKey];

		_reportSubject = config[Consts.ReportSubjectConfigKey];
		_fromEmail = config[Consts.ReportFromAddressConfigKey];
		_toEmail = config[Consts.ReportToAddressesConfigKey];

		_serviceBusClient = new(sbConnectionString, azureCredential);
		_serviceBusSender = _serviceBusClient.CreateSender(Consts.MammonServiceBusTopicName);

		var _adxHostAddress = config["AzureDataExplorer:HostAddress"];

		var kcsb = new KustoConnectionStringBuilder(_adxHostAddress, "devops")
			.WithAadTokenProviderAuthentication(async () =>
				(await azureCredential.GetTokenAsync(new(["https://kusto.kusto.windows.net/.default"]), cancellationToken: _cancellationToken)).Token);

		_cslQueryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
	}

	[TestMethod]
	public async Task WorkflowFinishesAndSendsEmailAsync()
	{
		//send report request to SB Topic to wake up Mammon instance
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
		emailData.FromEmail.Should().Be(_fromEmail);
		emailData.FromName.Should().Be(_fromEmail);
		emailData.Attachments.Should().NotBeEmpty();

		var expectedToContacts = _toEmail!.Split(';').Select(e => new Contact { Name = e, Email = e });
		emailData.ToList.Should().BeEquivalentTo(expectedToContacts);
	}

	public class EmailData 
	{
		public required string FromEmail { get; set; }

		public required string FromName { get; set; }

		public required string Attachments { get; set; }

		public IList<Contact>? ToList => JsonSerializer.Deserialize<IList<Contact>>(To);

		public required string To { get; set; }
	}
}
