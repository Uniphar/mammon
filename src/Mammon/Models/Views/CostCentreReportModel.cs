namespace Mammon.Models.Views;

public class CostCentreReportModel
{
	public CostCentreReportNode Root { get; private set; } = new CostCentreReportNode { Name = "Root", Parent = null };
	public string ReportId { get; set; } = string.Empty;
	public DateTime ReportFromDateTime { get; set; }
	public DateTime ReportToDateTime { get; set; }

	public void AddLeaf(string costCentre, string rgName, string environment, ResourceCost cost, string? nodeClass)
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

		rgNode.Leaves.TryAdd(environment, new CostCentreReportLeaf { Name= environment, Cost = cost, Parent = rgNode, CostCentreNode = costCentreNode });

		ComputeTotal(rgNode);

		if (!string.IsNullOrWhiteSpace(nodeClass))
			ComputeTotal(node);

		ComputeTotal(costCentreNode);	
	}

	private static void ComputeTotal(CostCentreReportNode node)
	{
		var list = node.Leaves.Select(x => x.Value.Cost).ToList();
		list.AddRange(node.SubNodes.Select(x => x.Value.NodeTotal));

		node.NodeTotal = new ResourceCost(list);
	}
}

public record CostCentreReportNode
{
	public IDictionary<string, CostCentreReportNode> SubNodes { get; private set; } = new Dictionary<string, CostCentreReportNode>();
	public IDictionary<string, CostCentreReportLeaf> Leaves { get; private set; } = new Dictionary<string, CostCentreReportLeaf>();
	public required string Name { get; set; }
	public ResourceCost NodeTotal { get; set; } = new ResourceCost { Cost = 0, Currency = string.Empty };
	public CostCentreReportNodeType Type { get; set; }
	public required CostCentreReportNode? Parent { get; set; }
}

public record CostCentreReportLeaf
{
	public required string Name { get; set; }
	public required ResourceCost Cost { get; set; }
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
