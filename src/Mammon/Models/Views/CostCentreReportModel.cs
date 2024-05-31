namespace Mammon.Models.Views;

public class CostCentreReportModel
{
	public CostCentreReportNode Root { get; private set; } = new CostCentreReportNode { Name = "Root", Parent = null };
	public string ReportId { get; set; } = string.Empty;
	public DateTime ReportFromDateTime { get; set; }
	public DateTime ReportToDateTime { get; set; }

	public void AddLeaf(string costCentre, string rgName, string environment, double cost, string? nodeClass)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(costCentre);
		ArgumentException.ThrowIfNullOrWhiteSpace(rgName);
		ArgumentException.ThrowIfNullOrWhiteSpace(environment);

		CostCentreReportNode? node;

		if (!Root.SubNodes.TryGetValue(costCentre, out CostCentreReportNode? costCentreNode))
		{
			costCentreNode = new CostCentreReportNode() {Name = costCentre, Type = CostCentreReportNodeType.CostCentre, Parent = Root};
			Root.SubNodes.Add(costCentre, costCentreNode);
		}

		if (!string.IsNullOrWhiteSpace(nodeClass))
		{
			if (!costCentreNode.SubNodes.TryGetValue(nodeClass, out node))
			{
				node = new CostCentreReportNode() {Name = nodeClass, Type = CostCentreReportNodeType.Group, Parent = costCentreNode };

				costCentreNode.SubNodes.TryAdd(nodeClass, node);
			}			
		}
		else
		{
			node = costCentreNode;
		}


		if (!node.SubNodes.TryGetValue(rgName, out CostCentreReportNode? rgNode))
		{
			rgNode = new CostCentreReportNode() {Name = rgName, Type = CostCentreReportNodeType.RG, Parent = node };
			node.SubNodes.Add(rgName, rgNode);
		}

		rgNode.Leaves.Add(new CostCentreReportLeaf { Name= environment, Value = cost, Parent = rgNode, CostCentreNode = costCentreNode });
		ComputeTotal(rgNode);

		if (!string.IsNullOrWhiteSpace(nodeClass))
			ComputeTotal(node);

		ComputeTotal(costCentreNode);	
	}

	private static void ComputeTotal(CostCentreReportNode node)
	{
		node.NodeTotal = node.Leaves.Sum(x => x.Value) + node.SubNodes.Sum(x => x.Value.NodeTotal);
	}
}

public record CostCentreReportNode
{
	public IDictionary<string, CostCentreReportNode> SubNodes { get; private set; } = new Dictionary<string, CostCentreReportNode>();
	public IList<CostCentreReportLeaf> Leaves { get; private set; } = [];	
	public required string Name { get; set; }
	public double NodeTotal { get; set; }
	public CostCentreReportNodeType Type { get; set; }
	public required CostCentreReportNode? Parent { get; set; }
}

public record CostCentreReportLeaf
{
	public required string Name { get; set; } = string.Empty;
	public required double Value { get; set; }
	public required CostCentreReportNode Parent { get; set; }
	public required CostCentreReportNode CostCentreNode { get; set; }
}

public enum CostCentreReportNodeType
{
	Root,
	CostCentre,
	Group,
	RG
}
