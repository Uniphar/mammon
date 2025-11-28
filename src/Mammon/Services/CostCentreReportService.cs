namespace Mammon.Services;

public class CostCentreReportService(
    IConfiguration configuration,
    CostCentreRuleEngine costCentreRuleEngine,
    CostCentreService costCentreService,
    ServiceBusClient serviceBusClient,
    IServiceProvider sp,
    TimeProvider timeProvider,
    BlobServiceClient blobServiceClient)
{
    private string EmailSubject => configuration[Consts.ReportSubjectConfigKey] ?? string.Empty;
    private string BlobStorageContainerName => configuration[Consts.DotFlyerAttachmentsContainerNameConfigKey]!;
    private IEnumerable<string> EmailToAddresses => configuration[Consts.ReportToAddressesConfigKey]!.SplitEmailContacts();
    private string EmailFromAddress => configuration[Consts.ReportFromAddressConfigKey]!;
    private int ReportBillingPeriodStartDayInMonth => int.Parse(configuration[Consts.ReportBillingPeriodStartDayInMonthConfigKey]!);

    public async Task<(string reportBody, string attachmentUri)> GenerateReportAsync(CostReportRequest reportRequest)
    {
        ArgumentNullException.ThrowIfNull(reportRequest);

        var subscriptionIds = costCentreRuleEngine.Subscriptions.Select(t => t.SubscriptionId);

        var costCentreStates = new Dictionary<string, CostCentreActorState>();

        foreach(var subId in subscriptionIds)
        {
            var subCostCentreStates = await costCentreService.RetrieveCostCentreStatesAsync(reportRequest.ReportId, subId);

            foreach(var subCostCentreState in subCostCentreStates)
            {
                if (costCentreStates.ContainsKey(subCostCentreState.Key))
                {
                    //merge costs
                    var existing = costCentreStates[subCostCentreState.Key];

                    var costsToAdd = new List<ResourceCost>();
                    if (existing.TotalCost is not null) costsToAdd.Add(existing.TotalCost);
                    if (subCostCentreState.Value.TotalCost is not null) costsToAdd.Add(subCostCentreState.Value.TotalCost);

                    if (costsToAdd.Count == 0) continue;

                    existing.TotalCost = new ResourceCost(costsToAdd);

                    if (existing.ResourceCosts is null)
                    {
                        existing.ResourceCosts = subCostCentreState.Value.ResourceCosts;
                    }
                    else if (subCostCentreState.Value.ResourceCosts is not null)
                    {
                        foreach(var resourceCost in subCostCentreState.Value.ResourceCosts)
                        {
                            if (existing.ResourceCosts.TryGetValue(resourceCost.Key, out var existingCost))
                            {
                                existingCost = new ResourceCost([existingCost, resourceCost.Value]);
                                existing.ResourceCosts[resourceCost.Key] = existingCost;
                            }
                            else
                            {
                                existing.ResourceCosts[resourceCost.Key] = resourceCost.Value;
                            }
                        }
                    }

                    if (existing.DevOpsProjectCosts is null)
                    {
                        existing.DevOpsProjectCosts = subCostCentreState.Value.DevOpsProjectCosts;
                }
                    else if (subCostCentreState.Value.DevOpsProjectCosts is not null)
                    {
                        foreach(var devOpsProject in subCostCentreState.Value.DevOpsProjectCosts)
                        {
                            if (existing.DevOpsProjectCosts.TryGetValue(devOpsProject.Key, out var existingDevOpsProject))
                            {
                                foreach(var devOpsProjectGroupContributor in devOpsProject.Value)
                                {
                                    if (existingDevOpsProject.TryGetValue(devOpsProjectGroupContributor.Key, out var existingDevOpsProjectGroupContributor))
                                    {
                                        existingDevOpsProjectGroupContributor = new ResourceCost([existingDevOpsProjectGroupContributor, devOpsProjectGroupContributor.Value]);
                                    }
                else
                {
                                        existingDevOpsProject.Add(devOpsProjectGroupContributor.Key, devOpsProjectGroupContributor.Value);
                                    }
                                }
                            }
                            else
                            {
                                existing.DevOpsProjectCosts[devOpsProject.Key] = devOpsProject.Value;
                            }
                        }
                    }
                }
                else
                {
                    costCentreStates[subCostCentreState.Key] = subCostCentreState.Value;
                }
            }
        }

        var viewModel = BuildViewModel(reportRequest, costCentreStates);

        //report body	
        var reportBody = await ViewRenderer.RenderViewToStringAsync("EmailReport", viewModel, ControllerContext);

        //attachment
        var attachmentUri = await GenerateCSVAttachmentAsync(viewModel);

        return (reportBody, attachmentUri);
    }

    private async Task<string> GenerateCSVAttachmentAsync(CostCentreReportModel model)
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

        var blobClient = blobServiceClient.GetBlobContainerClient(BlobStorageContainerName);
        streamWriter.Flush();
        stream.Position = 0;

        var blobName = $"{model.ReportId}_{Guid.NewGuid()}.csv";

        var response = await blobClient.UploadBlobAsync(blobName, BinaryData.FromStream(stream));

        return $"{blobClient.Uri.AbsoluteUri}/{blobName}";
    }

    private static void AppendNodeToCsv(CostCentreReportNode node, CsvWriter csv)
    {
        foreach (var subNode in node.SubNodes)
        {
            AppendNodeToCsv(subNode.Value, csv);
        }

        foreach (var leaf in node.Leaves)
        {
            var leafNode = leaf.Value;
            var parentNode = leafNode.Parent;
            var nodeClass = GenerateGroupingStringForCSVRow(parentNode);

			csv.WriteRecord(new CsvReportLine { Resource = parentNode.Name, Environment = leafNode.Name, Cost = leafNode.Cost, CostCentre = leafNode.CostCentreNode.Name, Grouping = nodeClass});
            csv.NextRecord();
        }
    }

    private static string GenerateGroupingStringForCSVRow(CostCentreReportNode node)
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
                sb.Append($"{(!first ? "/" : "")}{processedNode.Name}");
                first = false;
            }

            processedNode = processedNode.Parent;
        } while (processedNode != null && processedNode.Type != CostCentreReportNodeType.Root);

        return sb.ToString();
    }

    public static void ValidateConfiguration(IConfiguration configuration)
    {
        new CostCentreReportServiceConfigValidator().ValidateAndThrow(configuration);
    }

    public CostCentreReportModel BuildViewModel(CostReportRequest reportRequest, IDictionary<string, CostCentreActorState> costCentreStates)
    {
        ArgumentNullException.ThrowIfNull(reportRequest);
        ArgumentNullException.ThrowIfNull(costCentreStates);

        CostCentreReportModel emailReportModel = new() { ReportId = reportRequest.ReportId, ReportFromDateTime = reportRequest.CostFrom, ReportToDateTime = reportRequest.CostTo };

        foreach (var costCentre in costCentreStates)
        {
            var resourceCostsPivots = costCentre.Value.ResourceCosts?.Select(x => costCentreRuleEngine.ProjectCostReportPivotEntry(x.Key, x.Value));
            if (resourceCostsPivots != null)
            {
                var pivotGroups = resourceCostsPivots.GroupBy(x => (x.PivotName, x.SubscriptionId)).ToList();

                pivotGroups.Sort(new PivotDefinitionComparer());

                foreach (var pivotGroup in pivotGroups)
                {
                    var pivotName = pivotGroup.Key.PivotName;

                    var nodeClass = costCentreRuleEngine.ClassifyPivot(pivotGroup.First());
                    var environment = costCentreRuleEngine.LookupEnvironment(pivotGroup.Key.SubscriptionId);

                    emailReportModel.AddLeaf(costCentre.Key, pivotName, environment, new ResourceCost(pivotGroup.Select(x => x.Cost), true), nodeClass);
                }
            }

            if (costCentre.Value.DevOpsProjectCosts is not null)
            {
                foreach(var devOpsProjectCost in costCentre.Value.DevOpsProjectCosts)
                {
                    var projectName = devOpsProjectCost.Key;
                    foreach (var envCost in devOpsProjectCost.Value)
                    {
                        var environment = envCost.Key;
                        var cost = envCost.Value;
                        emailReportModel.AddLeaf(costCentre.Key, projectName, environment, cost, "DevOps Project");
                    }
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
            From = new Contact { Email = EmailFromAddress, Name = EmailFromAddress },
            Subject = string.Format(EmailSubject, reportRequest.ReportId),
            To = EmailToAddresses.Select(x => new Contact { Email = x, Name = x }).ToList(),
            //To = [new Contact() { Email = "aandrei@uniphar.ie", Name = "Andrei Andrei" }], //TODO: remove test address]
            Attachments = [attachmentUri]
        };

        await serviceBusClient.CreateSender("dotflyer-email").SendMessageAsync(new ServiceBusMessage
        {
            Body = BinaryData.FromObjectAsJson(dotFlyerRequest),
            ContentType = "application/json",
#if (DEBUG)
            MessageId = $"MammonEmailReport{Guid.NewGuid()}"
#else
			MessageId = $"MammonEmailReport{reportRequest.ReportId}"
#endif
        });
    }

    private ControllerContext ControllerContext
    {
        get
        {
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

        var first = new DateTime(now.Year, now.Month, ReportBillingPeriodStartDayInMonth, 0, 0, 0)
            .AddMonths(-1);

        var last = new DateTime(now.Year, now.Month, ReportBillingPeriodStartDayInMonth, 0, 0, 0)
            .AddSeconds(-1);

        return new CostReportRequest { CostFrom = first, CostTo = last, ReportId = first.ToString("yyMM") };
    }

    private class PivotDefinitionComparer : IComparer<IGrouping<(string pivotName, string subId), CostReportPivotEntry>>
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
        public required ResourceCost Cost { get; set; }
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

            RuleFor(x => x[Consts.ReportBillingPeriodStartDayInMonthConfigKey]).NotEmpty().Must(x => int.TryParse(x, out var parsed) && parsed >= 1 && parsed <= 31)
                .WithMessage("Cost Centre Report Billing Period Start Day In Month must be a number between 1 and 31 - inclusive");
        }
    }
}