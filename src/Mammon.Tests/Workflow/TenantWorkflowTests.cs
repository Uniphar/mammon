using CsvHelper.Configuration;
using Kusto.Cloud.Platform.Modularization;
using Mammon.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Polly.Extensions.Http;
using Westwind.Utilities.Extensions;
using static Mammon.Services.CostCentreReportService;

namespace Mammon.Tests.Workflow;

[TestClass, TestCategory("IntegrationTest")]
public class TenantWorkflowTests
{
	private static ServiceBusClient? _serviceBusClient;
	private static ServiceBusSender? _serviceBusSender;
	private static BlobServiceClient? _blobServiceClient;
	private static BlobContainerClient? _blobContainerClient;
	private static ICslQueryProvider? _cslQueryProvider;
	private static CostCentreRuleEngine? _costCentreRuleEngine;
	private static CostRetrievalService? _costRetrievalService;
	private static IHost? _host;
	private static readonly CostReportRequest costReportRequest = new()
	{
		ReportId = Guid.NewGuid().ToString(),
		CostFrom = DateTime.UtcNow.BeginningOfDay().AddDays(-2),
		CostTo = DateTime.UtcNow.BeginningOfDay().AddDays(-1)
	};
	private static CostReportRequest ReportRequest = costReportRequest;
	private static string? _reportSubject = string.Empty;
	private static string? _fromEmail = string.Empty;
	private static string? _toEmail = string.Empty;
	private static CancellationToken _cancellationToken;

	[ClassInitialize]
	public static void Initialize(TestContext testContext)
	{
		_cancellationToken = testContext.CancellationTokenSource.Token;

		DefaultAzureCredential defaultAzureCredential = new();

		var kvUrl = Environment.GetEnvironmentVariable(Consts.ConfigKeyVaultConfigEnvironmentVariable);
		ArgumentException.ThrowIfNullOrWhiteSpace(kvUrl, nameof(kvUrl));

		DefaultAzureCredential azureCredential = new();

		/* the path matches two repos side by side checkout and running tests under standard bin/{config}/{netx.y}
		 * this is the expected directory structure in local dev as well as the setup in CI/CD pipelines
		 */
		var inMemorySettings = new List<KeyValuePair<string, string>> {
			new(Consts.CostCentreRuleEngineFilePathConfigKey, "../../../../../../costcentre-definitions/costCentreRules.json")
		};

		HostApplicationBuilder builder = new HostApplicationBuilder();
		var policy = HttpPolicyExtensions
		.HandleTransientHttpError() // HttpRequestException, 5XX and 408
		.OrResult(response => (int)response.StatusCode == 429) // RetryAfter
		.AddCostManagementRetryPolicy();

		builder.Services
			.AddTransient<AzureAuthHandler>()
			.AddHttpClient("costRetrieval")
			.AddHttpMessageHandler<AzureAuthHandler>()
			.AddPolicyHandler(policy);

		_host = builder.Build();
		
		ConfigurationBuilder configurationBuilder = new();
		configurationBuilder.AddAzureKeyVault(new Uri(kvUrl), azureCredential);
		configurationBuilder.AddInMemoryCollection(inMemorySettings!);
		var config = configurationBuilder.Build();

		var sbConnectionString = config[Consts.DotFlyerSBConnectionStringConfigKey];

		_reportSubject = config[Consts.ReportSubjectConfigKey];
		ArgumentException.ThrowIfNullOrWhiteSpace(_reportSubject);

		_fromEmail = config[Consts.ReportFromAddressConfigKey];
		ArgumentException.ThrowIfNullOrWhiteSpace(_fromEmail);

		_toEmail = config[Consts.ReportToAddressesConfigKey];
		ArgumentException.ThrowIfNullOrWhiteSpace(_toEmail);

		_serviceBusClient = new(sbConnectionString, azureCredential);
		_serviceBusSender = _serviceBusClient.CreateSender(Consts.MammonServiceBusTopicName);

		var blobStorageConnectionString = config[Consts.DotFlyerAttachmentsBlobStorageConnectionStringConfigKey]!;
		_blobServiceClient = new(new Uri(blobStorageConnectionString), defaultAzureCredential);

		var BlobStorageContainerName = config[Consts.DotFlyerAttachmentsContainerNameConfigKey]!;
		_blobContainerClient = _blobServiceClient.GetBlobContainerClient(BlobStorageContainerName);
		_costCentreRuleEngine = new(config);

		var httpClientFactory = _host.Services.GetRequiredService<IHttpClientFactory>();
		
		_costRetrievalService = new(new ArmClient(defaultAzureCredential), httpClientFactory.CreateClient("costRetrieval"), Mock.Of<ILogger<CostRetrievalService>>(), config);

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

		await _serviceBusSender!.SendMessageAsync(new ServiceBusMessage
		{
			Body = BinaryData.FromObjectAsJson(new
			{
				data = ReportRequest
			}),
			ContentType = "application/json",
		});

		//wait for ADX to record email produced
		string expectedSubject = string.Format(_reportSubject!, ReportRequest.ReportId);

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
		var apiTotal = await ComputeCostAPITotalAsync();
		var total = await ComputeCSVReportTotalAsync(emailData.AttachmentsList!.First().Uri);

		decimal.Round(apiTotal, 2).Should().Be(decimal.Round(total, 2));			
	}

	private async Task<decimal> ComputeCostAPITotalAsync()
	{
		decimal total = 0m;

		foreach (var subscription in _costCentreRuleEngine!.SubscriptionNames)
		{
			var request = new CostReportSubscriptionRequest
			{
				SubscriptionName = subscription,
				ReportRequest = ReportRequest,
				GroupingMode = GroupingMode.Subscription
			};

			var response = await _costRetrievalService!.QueryForSubAsync(request);
			total += response.TotalCost;
		}

		return total;
	}

	private async Task<decimal> ComputeCSVReportTotalAsync(string uri)
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
