namespace Mammon.Services;

public class CostCentreReportService (IConfiguration configuration, CostCentreRuleEngine costCentreRuleEngine, ServiceBusClient serviceBusClient, IServiceProvider sp, TimeProvider timeProvider, BlobServiceClient blobServiceClient)
{
	private string EmailSubject => configuration[Consts.ReportSubjectConfigKey] ?? string.Empty;
	private string BlobStorageContainerName => configuration[Consts.DotFlyerAttachmentsContainerNameConfigKey]!;
	private IEnumerable<string> EmailToAddresses => configuration[Consts.ReportToAddressesConfigKey]!.Split(',');
	private string EmailFromAddress => configuration[Consts.ReportFromAddressConfigKey]!;

	public async Task<(string reportBody, string attachmentUri)> GenerateReportAsync(CostReportRequest reportRequest)
	{
		ArgumentNullException.ThrowIfNull(reportRequest);
		
		Dictionary<string, CostCentreActorState> costCentreStates = await RetrieveCostCentreStates(reportRequest.ReportId);

		var viewModel = BuildViewModel(reportRequest, costCentreStates);

		//report body	
		var reportBody = await ViewRenderer.RenderViewToStringAsync("EmailReport", viewModel, ControllerContext);

		//attachment
		var attachmentUri = await RenderCSVAsync(viewModel);

		return (reportBody, attachmentUri);
	}

	private async Task<string> RenderCSVAsync(CostCentreReportModel model)
	{
		ArgumentNullException.ThrowIfNull(model);

		using var stream = new MemoryStream();
		using var streamWriter = new StreamWriter(stream);

		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = true,
		};

		using var csvWriter = new CsvWriter(streamWriter, config);

		csvWriter.WriteComment($"From : {model.ReportFromDateTime:dd/MM/yyyy} (inclusive) To: {model.ReportToDateTime:dd/MM/yyyy} (inclusive) ");
		csvWriter.NextRecord();
		csvWriter.NextRecord();
		csvWriter.WriteHeader<CsvReportLine>();
		csvWriter.NextRecord();

		AppendNodeToCsv(model.Root, csvWriter);

		var blocClient = blobServiceClient.GetBlobContainerClient(BlobStorageContainerName);
		streamWriter.Flush();
		stream.Position = 0;

		var blobName = $"{model.ReportId}_{Guid.NewGuid()}.csv";

		var response = await blocClient.UploadBlobAsync(blobName, BinaryData.FromStream(stream));

		return $"{blocClient.Uri.AbsoluteUri}/{blobName}";
		
	}

	private static void AppendNodeToCsv(CostCentreReportNode node, CsvWriter csv)
	{
		foreach (var subNode in node.SubNodes)
		{
			AppendNodeToCsv(subNode.Value, csv);
		}

		foreach (var leaf in node.Leaves)
		{
			var parent = leaf.Parent;
			var nodeClass = GenerateGroupForCSVRow(leaf.Parent);

			csv.WriteRecord(new CsvReportLine { Resource = parent.Name, Environment = leaf.Name, Cost = leaf.Value, CostCentre = leaf.CostCentreNode.Name, Grouping = nodeClass});
			csv.NextRecord();
		}	
	}

	private static string GenerateGroupForCSVRow(CostCentreReportNode node)
	{
		if (node.Type == CostCentreReportNodeType.Root)
			return "";

		StringBuilder sb = new();

		var processedNode = node;
		bool first = true;
		do
		{
			if (processedNode.Type == CostCentreReportNodeType.Group)
			{
				sb.Append($"{(!first ? "/": "")}{processedNode.Name}");
				first = false;
			}
			processedNode = processedNode.Parent;
		} while (processedNode!=null && processedNode.Type != CostCentreReportNodeType.Root);

		return sb.ToString();
	}

	private async Task<Dictionary<string, CostCentreActorState>> RetrieveCostCentreStates(string reportId)
	{
		var costCentres = costCentreRuleEngine.CostCentres;

		Dictionary<string, CostCentreActorState> costCentreStates = [];

		foreach (var costCentre in costCentres)
		{
			var state = await ActorProxy.DefaultProxyFactory.CallActorWithNoTimeout<ICostCentreActor, CostCentreActorState>(CostCentreActor.GetActorId(reportId, costCentre), nameof(CostCentreActor), async (p) => await p.GetCostsAsync());
			if (state != null)
			{
				costCentreStates.Add(costCentre, state);
			}
		}

		return costCentreStates;
	}

	public static void ValidateConfiguration(IConfiguration configuration)
	{
		new CostCentreReportServiceConfigValidator().ValidateAndThrow(configuration);
	}

	public CostCentreReportModel BuildViewModel(CostReportRequest reportRequest, IDictionary<string, CostCentreActorState> costCentreStates)
	{
		ArgumentNullException.ThrowIfNull(reportRequest);
		ArgumentNullException.ThrowIfNull(costCentreStates);

		CostCentreReportModel emailReportModel = new() {ReportId = reportRequest.ReportId, ReportFromDateTime = reportRequest.CostFrom, ReportToDateTime = reportRequest.CostTo };

		foreach (var costCentre in costCentreStates)
		{
			var pivots = costCentre.Value.ResourceCosts?.Select(x => costCentreRuleEngine.ProjectCostReportPivotEntry(x.Key, x.Value));
			if (pivots != null)
			{
				var pivotGroups = pivots.GroupBy(x => (x.PivotName, x.SubscriptionId)).ToList();

				pivotGroups.Sort(new PivotDefinitionComparer());
				foreach (var pivotGroup in pivotGroups)
				{
					var pivotName = pivotGroup.Key.PivotName;

					var nodeClass = costCentreRuleEngine.ClassifyPivot(pivotGroup.First());
					var environment = costCentreRuleEngine.LookupEnvironment(pivotGroup.Key.SubscriptionId);

					emailReportModel.AddLeaf(costCentre.Key, pivotName, environment, pivotGroup.Sum(x => x.Cost), nodeClass);
				}
			}
		}

		return emailReportModel;
	}

	public async Task SendReportToDotFlyerAsync(CostReportRequest reportRequest)
	{
		//generate report body and its attachment
		var (reportBody, attachmentUri) = await GenerateReportAsync(reportRequest);	

		//send request to DotFlyer
		var dotFlyerRequest = new EmailMessage
		{
			Body = reportBody,
			From = new Contact { Email = EmailFromAddress , Name = EmailFromAddress },
			Subject = string.Format(EmailSubject, reportRequest.ReportId),
			To = EmailToAddresses.Select(x => new Contact { Email = x, Name = x }).ToList(),
			Attachments= [attachmentUri]
		};

		await serviceBusClient.CreateSender("dotflyer-email").SendMessageAsync(new ServiceBusMessage
		{
			Body = BinaryData.FromObjectAsJson(dotFlyerRequest),
			ContentType = "application/json",
			MessageId = $"MammonEmailReport{reportRequest.ReportId}"
		});
	}

	private ControllerContext ControllerContext
	{
		get {
			var httpContext = new DefaultHttpContext();

			var currentUri = new Uri("http://localhost"); //we will not generate any external links
			httpContext.Request.Scheme = currentUri.Scheme;
			httpContext.Request.Host = HostString.FromUriComponent(currentUri);
			httpContext.RequestServices = sp;
			var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
			var ctx = new ControllerContext(actionContext);

			return ctx;
		}
	}

	public CostReportRequest GenerateDefaultReportRequest()
	{
		var now = timeProvider.GetLocalNow();

		var month = new DateTime(now.Year, now.Month, 1, 0,0,0);
		var first = month.AddMonths(-1);
		var last = month.AddSeconds(-1);

		return new CostReportRequest { CostFrom = first, CostTo = last, ReportId = first.ToString("yyMM") };
	}

	private class PivotDefinitionComparer : IComparer<IGrouping<(string pivotName,string subId), CostReportPivotEntry>>
	{
		public int Compare(IGrouping<(string pivotName, string subId), CostReportPivotEntry>? x, IGrouping<(string pivotName, string subId), CostReportPivotEntry>? y)
		{
			if (x == null && y == null)
				return 0;
			else if (x == null)
				return -1;
			else if (y == null)
				return 1;
			else
				return string.CompareOrdinal(x.Key.pivotName, y.Key.pivotName);
		}
	}

	public record CsvReportLine
	{
		public required string Resource { get; set; }
		public required string Environment { get; set; }
		public required double Cost { get; set; }
		public string Currency { get; set; } = "EUR"; //this will be populated from response later
		public required string CostCentre { get; set; }
		public required string Grouping { get; set; }
	}

	internal class CostCentreReportServiceConfigValidator : AbstractValidator<IConfiguration>
	{
		public CostCentreReportServiceConfigValidator()
		{
			RuleFor(x => x[Consts.ReportToAddressesConfigKey]).NotEmpty()
				.WithMessage("Cost Centre Report Service To Addresses list must not be empty");

			RuleFor(x => x[Consts.ReportSubjectConfigKey]).NotEmpty()
				.WithMessage("Cost Centre Report Service Subject must not be empty");

			RuleFor(x => x[Consts.ReportFromAddressConfigKey]).NotEmpty()
				.WithMessage("Cost Centre Report Service From Address must not be empty");

			RuleFor(x => x[Consts.DotFlyerSBConnectionStringConfigKey]).NotEmpty()
				.WithMessage("Cost Centre Report Service Bus Uri must not be empty");

			RuleFor(x => x[Consts.DotFlyerAttachmentsBlobStorageConnectionStringConfigKey]).NotEmpty()
				.WithMessage("DotFlyer blob storage connection string must not be empty");

			RuleFor(x => x[Consts.DotFlyerAttachmentsContainerNameConfigKey]).NotEmpty()
				.WithMessage("DotFlyer blob storage container name must not be empty");
		}
	}
}
