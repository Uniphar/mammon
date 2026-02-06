namespace Mammon.Tests.Workflow;

[TestClass]
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
        CostFrom = DateTime.UtcNow.BeginningOfDay().AddDays(-17),
        CostTo = DateTime.UtcNow.BeginningOfDay().AddDays(-16)
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
		var costCentreDevOpsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "../../../../../../costcentre-definitions/costCentreDevOpsRules.json");

		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.CostCentreRuleEngineFilePathConfigKey, costCentreFile),
			new(Consts.CostCentreRuleEngineDevOpsConfigKey, costCentreDevOpsFile)
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

		builder.Services
			.AddTransient<AzureDevOpsAuthHandler>()
			.AddHttpClient("azureDevOpsHttpClient")
			.AddHttpMessageHandler<AzureDevOpsAuthHandler>();

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


    [TestMethod, TestCategory("MockedIntegrationTest")]
    public async Task WorkflowFinishesWithMockData_EmailIsSentAndTotalsMatch()
	{
		var expectedResourcesTotal = 11675.26m;
		var expectedDevOpsLicensesTotal = 400.0m;
		var expectedVisualStudioSubscriptionsCostTotal = 2400.0m;
		decimal expectedTotal = expectedResourcesTotal + expectedDevOpsLicensesTotal + expectedVisualStudioSubscriptionsCostTotal;

        await _serviceBusSender!.SendMessageAsync(new ServiceBusMessage
        {
            Body = BinaryData.FromObjectAsJson(new
            {
                data = _reportRequest
            }),
            ContentType = "application/json",
        }, TestContext.CancellationTokenSource.Token);

        //wait for ADX to record email produced
        string expectedSubject = string.Format(_reportSubject!, _reportRequest.ReportId);

        EmailData emailData = await _cslQueryProvider!
            .WaitSingleQueryResult<EmailData>($"DotFlyerEmails | where Subject == \"{expectedSubject}\"", TimeSpan.FromMinutes(30), _cancellationToken);

        //assertions

        emailData.Should().NotBeNull();
        emailData.FromEmail.Should().Be(_fromEmail);
        emailData.FromName.Should().Be(_fromEmail);
        emailData.Attachments.Should().NotBeEmpty();
        emailData.AttachmentsList.Should().ContainSingle();

        //retrieve content and compute total
        var resultTotal = await ComputeCSVReportTotalAsync(emailData.AttachmentsList!.First().Uri);

		TestContext.WriteLine($"Received result total: {resultTotal} from file: {emailData.AttachmentsList!.First().Uri}");

        // this is to cover rounding issues
        var expectedTotalRound = decimal.Round(expectedTotal, 2);
        var resultTotalRound = decimal.Round(resultTotal, 2);

        Math.Abs(expectedTotalRound - resultTotalRound).Should().BeLessThan(1.0m, $"api total is {expectedTotalRound} and csv total is {resultTotalRound}");
    }

    [TestMethod, TestCategory("IntegrationTest")]
    public async Task WorkflowFinishesEmailSentTotalsMatch()
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

		// this is to cover rounding issues
		var apiTotalRound = decimal.Round(apiTotal, 2);
		var csvTotalRound = decimal.Round(csvTotal, 2);

		Math.Abs(apiTotalRound - csvTotalRound).Should().BeLessThan(4.0m, $"api total is {apiTotalRound} and csv total is {csvTotalRound}");
	}

	private static async Task<decimal> ComputeCostAPITotalAsync()
	{
		decimal total = 0m;

		foreach (var subscription in _costCentreRuleEngine!.Subscriptions)
		{
			var request = new ObtainCostsActivityRequest
			{
                ReportId = "some-report-id",
                SubscriptionName = subscription.SubscriptionName,
				CostFrom = _reportRequest.CostFrom,
				CostTo = _reportRequest.CostTo,
				PageIndex = 0,
				GroupingMode = GroupingMode.Subscription
			};

			var httpClientFactory = _host!.Services.GetRequiredService<IHttpClientFactory>();
			CostRetrievalService _costRetrievalService = new(new ArmClient(new DefaultAzureCredential()), httpClientFactory.CreateClient("costRetrievalHttpClient"), _config!, Mock.Of<ILogger<CostRetrievalService>>());

			var response = await _costRetrievalService!.QueryResourceCostForSubAsync(request);
			total += response.TotalCost;

			if (!string.IsNullOrWhiteSpace(subscription.DevOpsOrganization))
			{
				var devOpsRequest = new ObtainDevOpsCostsActivityRequest
				{
                    ReportId = "some-report-id",
                    SubscriptionName = subscription.SubscriptionName,
					CostFrom = _reportRequest.CostFrom,
					CostTo = _reportRequest.CostTo,
					DevOpsOrganization = subscription.DevOpsOrganization
				};

				var devOpsCost = await _costRetrievalService!.QueryDevOpsCostForSubAsync(devOpsRequest);

				decimal basicLicensesCost = devOpsCost.SingleOrDefault(t => t.Product == ObtainDevOpsCostsActivity.BasicLicenseProductName)?.Cost.Cost ?? 0m;
				total += basicLicensesCost;

                decimal testPlansLicensesCost = devOpsCost.SingleOrDefault(t => t.Product == ObtainDevOpsCostsActivity.BasicPlusTestPlansLicenseProductName)?.Cost.Cost ?? 0m;
				total += testPlansLicensesCost;
            }

			var visualStudioSubscriptionCostRequest = new ObtainVisualStudioSubscriptionCostActivityRequest
			{
                ReportId = "some-report-id",
                SubscriptionName = subscription.SubscriptionName,
				CostFrom = _reportRequest.CostFrom,
				CostTo = _reportRequest.CostTo
			};
			var visualStudioSubscriptionCost = await _costRetrievalService!.QueryVisualStudioSubscriptionCostForSubAsync(visualStudioSubscriptionCostRequest);
			if (visualStudioSubscriptionCost != null)
			{
                total += visualStudioSubscriptionCost.Sum(t => t.Cost.Cost);
            }
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

    public TestContext TestContext { get; set; }
}
