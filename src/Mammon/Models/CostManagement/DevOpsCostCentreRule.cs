namespace Mammon.Models.CostManagement;

public class DevOpsCostCentreRule
{
	public string CostCentre { get; set; } = string.Empty;
	public string ProjectNameMatchPattern { get; set; } = string.Empty;

	private Regex? _projectNameRegExp;
	public Regex? ProjectNameRegExp => GetRegExp(ProjectNameMatchPattern, ref _projectNameRegExp);

	public (int matchScore, DevOpsCostCentreRule matchedRule) Matches(string projectName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

		var projectNameMatch = ProjectNameRegExp == null || ProjectNameRegExp.IsMatch(projectName);

		if (projectNameMatch)
		{
			return (1, this);
        }

		return (-1, this);
    }

    private static Regex? GetRegExp(string? input, ref Regex? regex)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        return regex ??= new Regex(input, RegexOptions.Compiled);
    }
}
