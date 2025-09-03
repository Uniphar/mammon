namespace Mammon.Extensions;

public static class StringExtensions
{
	public static string ToParentResourceId(this string value)
	{
		const string provider = "/providers/";

		string[] removals = ["/extensions/"];

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

	public static string ToSHA256(this string value)
	{
		// Check if the input string is null or empty
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}

		// Create a SHA256 instance
		// Compute the hash as a byte array
		byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

		// Convert the byte array to a string
		StringBuilder builder = new();
		foreach (byte b in bytes)
		{
			builder.Append(b.ToString("x2"));
		}

		return builder.ToString();
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
