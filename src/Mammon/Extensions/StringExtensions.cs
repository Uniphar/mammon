namespace Mammon.Extensions;

public static class StringExtensions
{
    public static string ToParentResourceId(this string value) 
    {
        const string providers = "/providers";

        //we want to extract only the top level provider
        var firstIndex = value.IndexOf(providers, StringComparison.Ordinal);
        var lastIndex = value.LastIndexOf(providers, StringComparison.Ordinal);
     
        return firstIndex!=lastIndex ? value[..lastIndex] : value;
    }

    public static string RemoveSuffixes(this string value, IEnumerable<string> suffixes)
    {
        if (string.IsNullOrWhiteSpace(value) || suffixes==null || !suffixes.Any())
        {
            return value;
        }
        
        foreach ( var suffix in suffixes )
        {
            if (!string.IsNullOrWhiteSpace(suffix) && value.EndsWith(suffix))
            {
                value = value.Remove(value.LastIndexOf(suffix));
            }
        }

        return value;
    }
}
