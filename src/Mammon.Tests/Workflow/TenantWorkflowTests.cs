﻿namespace Mammon.Tests.Workflow;

[TestClass, TestCategory("IntegrationTest")]
public class TenantWorkflowTests
{
	private static ServiceBusClient? _serviceBusClient;
	private static ServiceBusSender? _serviceBusSender;
	private static BlobServiceClient? _blobServiceClient;
	private static BlobContainerClient? _blobContainerClient;
	private static ICslQueryProvider? _cslQueryProvider;
	private static CostCentreRuleEngine? _costCentreRuleEngine;
	private static IHost? _host;
	private static IConfiguration? _config;
	private static CostReportRequest _reportRequest = new()
	{
		ReportId = Guid.NewGuid().ToString(),
		CostFrom = DateTime.UtcNow.BeginningOfDay().AddDays(-2),
		CostTo = DateTime.UtcNow.BeginningOfDay().AddDays(-1)
	};

	private static string? _reportSubject = string.Empty;
	private static string? _fromEmail = string.Empty;
	private static string? _toEmail = string.Empty;
	private static CancellationToken _cancellationToken;


	[ClassInitialize]
	public static void Initialize(TestContext testContext)
	{
		_cancellationToken = testContext.CancellationTokenSource.Token;

		var kvUrl = Environment.GetEnvironmentVariable(Consts.ConfigKeyVaultConfigEnvironmentVariable);
		ArgumentException.ThrowIfNullOrWhiteSpace(kvUrl, nameof(kvUrl));

		DefaultAzureCredential azureCredential = new();

		/* the path matches two repos side by side checkout and running tests under standard bin/{config}/{netx.y}
		 * this is the expected directory structure in local dev as well as the setup in CI/CD pipelines
		 */

		var costCentreFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "../../../../../../costcentre-definitions/costCentreRules.json");

		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.CostCentreRuleEngineFilePathConfigKey, costCentreFile)
		};

		HostApplicationBuilder builder = new();

		var policy = HttpPolicyExtensions
		.HandleTransientHttpError() // HttpRequestException, 5XX and 408
		.OrResult(response => (int)response.StatusCode == 429) // RetryAfter
		.AddCostManagementRetryPolicy();

		builder.Services
			.AddTransient<AzureAuthHandler>()
			.AddHttpClient("costRetrievalHttpClient")
			.AddHttpMessageHandler<AzureAuthHandler>()
			.AddPolicyHandler(policy);

		_host = builder.Build();
		
		ConfigurationBuilder configurationBuilder = new();
		configurationBuilder.AddAzureKeyVault(new Uri(kvUrl), azureCredential);
		configurationBuilder.AddInMemoryCollection(inMemorySettings!);
		_config = configurationBuilder.Build();

		var sbConnectionString = _config[Consts.DotFlyerSBConnectionStringConfigKey];

		_reportSubject = _config[Consts.ReportSubjectConfigKey];
		ArgumentException.ThrowIfNullOrWhiteSpace(_reportSubject);

		_fromEmail = _config[Consts.ReportFromAddressConfigKey];
		ArgumentException.ThrowIfNullOrWhiteSpace(_fromEmail);

		_toEmail = _config[Consts.ReportToAddressesConfigKey];
		ArgumentException.ThrowIfNullOrWhiteSpace(_toEmail);

		_serviceBusClient = new(sbConnectionString, azureCredential);
		_serviceBusSender = _serviceBusClient.CreateSender(Consts.MammonServiceBusTopicName);

		var blobStorageConnectionString = _config[Consts.DotFlyerAttachmentsBlobStorageConnectionStringConfigKey]!;
		_blobServiceClient = new(new Uri(blobStorageConnectionString), azureCredential);

		var BlobStorageContainerName = _config[Consts.DotFlyerAttachmentsContainerNameConfigKey]!;
		_blobContainerClient = _blobServiceClient.GetBlobContainerClient(BlobStorageContainerName);

		_costCentreRuleEngine = new(_config);
		
		var _adxHostAddress = _config["AzureDataExplorer:HostAddress"];

		var kcsb = new KustoConnectionStringBuilder(_adxHostAddress, "devops")		
			.WithAadTokenProviderAuthentication(async () =>
				(await azureCredential.GetTokenAsync(new(["https://kusto.kusto.windows.net/.default"]), cancellationToken: _cancellationToken)).Token);

		_cslQueryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
	}

	[TestMethod]
	public async Task WorkflowFinishesAndSendsEmailAsync()
	{

		//send report request to SB Topic to wake up Mammon instance

		///workaround for https://github.com/Azure/azure-cli/issues/28708#issuecomment-2047256166
		///pre-access some things upfront

		await _blobContainerClient!.ExistsAsync();
		var apiTotal = await ComputeCostAPITotalAsync();

		await _serviceBusSender!.SendMessageAsync(new ServiceBusMessage
		{
			Body = BinaryData.FromObjectAsJson(new
			{
				data = _reportRequest
			}),
			ContentType = "application/json",
		});

		//wait for ADX to record email produced
		string expectedSubject = string.Format(_reportSubject!, _reportRequest.ReportId);
	
		EmailData emailData = await _cslQueryProvider!
			.WaitSingleQueryResult<EmailData>($"DotFlyerEmails | where Subject == \"{expectedSubject}\"", TimeSpan.FromMinutes(30), _cancellationToken);

		//assertions
		var expectedToContacts = _toEmail!
			.SplitEmailContacts()
			.Select(e => new Contact { Name = e, Email = e });

		emailData.ToList.Should().BeEquivalentTo(expectedToContacts);

		emailData.Should().NotBeNull();
		emailData.FromEmail.Should().Be(_fromEmail);
		emailData.FromName.Should().Be(_fromEmail);
		emailData.Attachments.Should().NotBeEmpty();
		emailData.AttachmentsList.Should().ContainSingle();
		emailData.ToList.Should().BeEquivalentTo(expectedToContacts);
	
		//retrieve content and compute total
		var csvTotal = await ComputeCSVReportTotalAsync(emailData.AttachmentsList!.First().Uri);
		
		decimal.Round(apiTotal, 2).Should().Be(decimal.Round(csvTotal, 2));	
	}

	private static async Task<decimal> ComputeCostAPITotalAsync()
	{
		decimal total = 0m;

		foreach (var subscription in _costCentreRuleEngine!.SubscriptionNames)
		{
			var request = new CostReportSubscriptionRequest
			{
				SubscriptionName = subscription,
				ReportRequest = _reportRequest,
				GroupingMode = GroupingMode.Subscription
			};

			var httpClientFactory = _host!.Services.GetRequiredService<IHttpClientFactory>();
			CostRetrievalService _costRetrievalService = new(new ArmClient(new DefaultAzureCredential()), httpClientFactory.CreateClient("costRetrievalHttpClient"), Mock.Of<ILogger<CostRetrievalService>>(), _config!);

			var response = await _costRetrievalService!.QueryForSubAsync(request);
			total += response.TotalCost;
		}

		return total;
	}

	private static async Task<decimal> ComputeCSVReportTotalAsync(string uri)
	{
		Uri blobUri = new(uri);
		var blobName = blobUri.Segments.Last();
		var fileName = Path.GetTempFileName();
		var stream = await _blobContainerClient!.GetBlobClient(blobName).DownloadToAsync(fileName);

		CsvConfiguration csvConfiguration = new(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = true,
			HeaderValidated = null,
			Delimiter = ",",
			IgnoreBlankLines = true,
			TrimOptions = TrimOptions.Trim,
			MissingFieldFound = (args) => {}
		};

		CsvReader csvReader = new(new StreamReader(fileName), csvConfiguration);
		csvReader.Read();
		var headerRead = csvReader.ReadHeader();
		csvReader.Read();
		csvReader.GetRecord<object>(); //comment row to be read

		decimal total = 0m;
		while (csvReader.Read())
		{
			var lineCost = Convert.ToDecimal(csvReader.GetField(2), CultureInfo.InvariantCulture);

			total += lineCost;
		}

		return total;
	}

	public class EmailData 
	{
		public required string FromEmail { get; set; }

		public required string FromName { get; set; }

		public required string Attachments { get; set; }

		public IList<Attachment>? AttachmentsList => JsonSerializer.Deserialize<IList<Attachment>>(Attachments);

		public IList<Contact>? ToList => JsonSerializer.Deserialize<IList<Contact>>(To);

		public required string To { get; set; }
	}

	public class Attachment 
	{
		[JsonPropertyName("URI")]
		public required string Uri { get; set; }
	}
}
