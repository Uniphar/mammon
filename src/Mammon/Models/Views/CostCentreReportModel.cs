namespace Mammon.Models.Views;

public class CostCentreReportModel
{
	public CostCentreReportNode Root { get; private set; } = new CostCentreReportNode();

	public void AddLeaf(string costCentre, string rgName, string environment, double cost, string? nodeClass)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(costCentre);
		ArgumentException.ThrowIfNullOrWhiteSpace(rgName);
		ArgumentException.ThrowIfNullOrWhiteSpace(environment);

		CostCentreReportNode? node;

		if (!Root.SubNodes.TryGetValue(costCentre, out CostCentreReportNode? costCentreNode))
		{
			costCentreNode = new CostCentreReportNode() { Type = CostCentreReportNodeType.CostCentre};
			Root.SubNodes.Add(costCentre, costCentreNode);
		}

		if (!string.IsNullOrWhiteSpace(nodeClass))
		{
			if (!costCentreNode.SubNodes.TryGetValue(nodeClass, out node))
			{
				node = new CostCentreReportNode() { Type = CostCentreReportNodeType.Group };

				costCentreNode.SubNodes.TryAdd(nodeClass, node);
			}			
		}
		else
		{
			node = costCentreNode;
		}


		if (!node.SubNodes.TryGetValue(rgName, out CostCentreReportNode? rgNode))
		{
			rgNode = new CostCentreReportNode() { Type = CostCentreReportNodeType.RG };
			node.SubNodes.Add(rgName, rgNode);
		}

		rgNode.Leaves.TryAdd(environment, cost);
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

public class CostCentreReportNode
{
	public IDictionary<string, CostCentreReportNode> SubNodes { get; private set; } = new Dictionary<string, CostCentreReportNode>();
	public IDictionary<string, double> Leaves { get; private set; } = new Dictionary<string, double>();	
	public double NodeTotal { get; set; }
	public CostCentreReportNodeType Type { get; set; }
}

public enum CostCentreReportNodeType
{
	Root,
	CostCentre,
	Group,
	RG
}

public class CostCentreReportLeaf
{
	public string Name { get; set; } = string.Empty;
	public double Cost { get; set; }
}
