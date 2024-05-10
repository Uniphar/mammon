using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Mammon.Models.Views;

public class EmailReportModel : PageModel
{
	public EmailReportNode Root { get; private set; } = new EmailReportNode();

	public void AddLeaf(string rgName, double cost, string? nodeClass)
	{
		EmailReportNode? node;
		if (!string.IsNullOrWhiteSpace(nodeClass))
		{
			if (!Root.SubNodes.TryGetValue(nodeClass, out node))
			{
				node = new EmailReportNode();

				Root.SubNodes.TryAdd(nodeClass, node);
			}			
		}
		else
		{
			node = Root;
		}

		_ = node.Leaves.TryAdd(rgName, cost);
	}
}

public class EmailReportNode
{
	public IDictionary<string, EmailReportNode> SubNodes { get; private set; } = new Dictionary<string, EmailReportNode>();
	public IDictionary<string, double> Leaves { get; private set; } = new Dictionary<string, double>();
	
}

public class EmailReportLeaf
{
	public string Name { get; set; } = string.Empty;
	public double Cost { get; set; }
}
