using System.Text.RegularExpressions;

namespace DotRun.WebServer;

public class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value == null) return null;

        // Convert to kebab case
        string transformed = Regex.Replace(
            value.ToString()!,
            "([a-z])([A-Z])",
            "$1-$2",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)
        ).ToLower();

        // if starting with api-, use api/ prefix
        if (transformed.StartsWith("api-"))
            transformed = "api/" + transformed.Substring(4);

        return transformed;
    }
}