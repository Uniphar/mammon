namespace Mammon.Extensions;

public static class StringExtensions
{
    public static string ToResourceActorId(this string value) 
    {
        const string providers = "/providers";

        //we want to extract only the top level provider
        var firstIndex = value.IndexOf(providers, StringComparison.Ordinal);
        var lastIndex = value.LastIndexOf(providers, StringComparison.Ordinal);
     
        return firstIndex!=lastIndex ? value[..lastIndex] : value;
    }
}
