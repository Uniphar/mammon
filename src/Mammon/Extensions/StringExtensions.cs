namespace Mammon.Extensions;

public static class StringExtensions
{
	public static string ToParentResourceId(this string value)
	{
		const string provider = "/providers/";

		string[] removals = { "/extensions/" };

		//we want to extract only the top level provider
		var firstIndex = value.IndexOf(provider, StringComparison.Ordinal);
		var lastIndex = value.LastIndexOf(provider, StringComparison.Ordinal);

		value = firstIndex != lastIndex ? value[..lastIndex] : value;

		foreach (var removal in removals)
		{
			firstIndex = value.IndexOf(removal, StringComparison.Ordinal);
			if (firstIndex != -1)
				value = value[..firstIndex];
		}

		return value;
	}

	public static string RemoveSuffixes(this string value, IEnumerable<string> suffixes)
	{
		if (string.IsNullOrWhiteSpace(value) || suffixes == null || !suffixes.Any())
		{
			return value;
		}

		foreach (var suffix in suffixes)
		{
			if (!string.IsNullOrWhiteSpace(suffix) && value.EndsWith(suffix))
			{
				value = value.Remove(value.LastIndexOf(suffix));
			}
		}

		return value;
	}

	public static IList<string> SplitEmailContacts(this string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return [];
		}

		return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
	}
}
