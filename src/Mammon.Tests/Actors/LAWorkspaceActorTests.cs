using Dapr.Actors.Runtime;
using Mammon.Actors;
using Mammon.Models.Workflows;

namespace Mammon.Tests.Actors;

public abstract class BaseUnitTests
{
    protected static CostCentreRuleEngine GetInstance()
    {
        var inMemorySettings = new List<KeyValuePair<string, string>> {
            new(Consts.CostCentreRuleEngineFilePathConfigKey, "./Services/testCostCentreRules.json")
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        return new CostCentreRuleEngine(configuration);
    }
}


[TestClass]
[TestCategory("UnitTest")]
public class LAWorkspaceActorTests : BaseUnitTests
{
    private readonly ActorHost _actorHost = ActorHost.CreateForTest(typeof(LAWorkspaceActor), "id");
    private readonly ILogger<LAWorkspaceActor> _logger = Mock.Of<ILogger<LAWorkspaceActor>>();
    private readonly Mock<ICostCentreService> _costCentreService = new();
    private readonly Mock<StateManagerService> _stateManagerService = new();
    private readonly Mock<IActorCaller> _actorCaller = new();

    private LAWorkspaceActor GetSut()
    {
        var costCentreRuleEngine = GetInstance();
        return new LAWorkspaceActor(
            _actorHost, 
            _logger, 
            _costCentreService.Object, 
            costCentreRuleEngine,
            _stateManagerService.Object,
            _actorCaller.Object);
    }

    [TestMethod]
    public async Task SplitCost_HappyPath()
    {
        // Arrange
        var request = new SplittableResourceRequest
        {
            ReportRequest = new CostReportRequest { ReportId = "report1" },
            Resource = new ResourceCostResponse
            {
                ResourceId = "/subscriptions/21a25c3f-776a-408f-b319-f43e54634695/resourcegroups/rgName/providers/microsoft.storage/storageaccounts/sampleSA",
                Cost = new ResourceCost(100, "USD"),
                Tags = new Dictionary<string, string> { { "tagAName", "tagAValue" }, { "tagBName", "tagBValue" } }
            }
        };
        var sut = GetSut();
        var data = new List<LAWorkspaceQueryResponseItem>
        {
            new() { SelectorType = Consts.ResourceIdLAWorkspaceSelectorType, Selector = request.Resource.ResourceId, SizeSum = 50 },
            new() { SelectorType = Consts.ResourceIdLAWorkspaceSelectorType, Selector = request.Resource.ResourceId, SizeSum = 50 }
        };
        _costCentreService.Setup(s => s.RetrieveCostCentreStatesAsync(request.ReportRequest.ReportId))
            .ReturnsAsync(new Dictionary<string, CostCentreActorState>
            {
                { 
                    "FullMatchSingleTag", 
                    new CostCentreActorState 
                    { 
                        ResourceCosts = new Dictionary<string, ResourceCost> 
                        { 
                            { 
                                request.Resource.ResourceId.ToParentResourceId(), 
                                new ResourceCost(0, "USD") 
                            }
                        }, 
                        TotalCost = new ResourceCost(0, "USD") 
                    } 
                }
            });

        _stateManagerService.Setup(t => t.TryGetStateAsync<CoreResourceActorState>(It.IsAny<IActorStateManager>(), LAWorkspaceActor.CostStateName))
            .ReturnsAsync(new ConditionalValue<CoreResourceActorState>(true, new CoreResourceActorState
            {
                ResourceId = request.Resource.ResourceId,
                TotalCost = new ResourceCost
                {
                    Cost = request.Resource.Cost.Cost,
                    Currency = request.Resource.Cost.Currency
                },
            }));

        // Act
        await sut.SplitCost(request, data);

        // Assert

        _costCentreService.Verify(t => t.RetrieveCostCentreStatesAsync(request.ReportRequest.ReportId), Times.Once);
        _costCentreService.VerifyNoOtherCalls();
        _stateManagerService.Verify(t => t.TryGetStateAsync<CoreResourceActorState>(It.IsAny<IActorStateManager>(), LAWorkspaceActor.CostStateName), Times.Once);
        _stateManagerService.Verify(t => t.SetStateAsync(It.IsAny<IActorStateManager>(), LAWorkspaceActor.CostStateName, 
            It.Is<CoreResourceActorState>(
                s => s.ResourceId == request.Resource.ResourceId 
                    && s.TotalCost.Cost == request.Resource.Cost.Cost 
                    && s.TotalCost.Currency == request.Resource.Cost.Currency)), Times.Once);
        _stateManagerService.VerifyNoOtherCalls();
        _actorCaller.Verify(t => t.CallAsync(It.Is<string>(id => id == CostCentreActor.GetActorId(request.ReportRequest.ReportId, "FullMatchSingleTag")), 
            nameof(CostCentreActor), 
            It.Is<Func<ICostCentreActor, Task>>(func => func != null)), Times.Once);
        _actorCaller.VerifyNoOtherCalls();
    }
}
