using System.Text.RegularExpressions;

namespace RentToBooks.Core;

public static partial class TextFormatting
{
    /// <summary>Strips tabs/newlines so a value is safe to place in a tab-delimited IIF line.</summary>
    public static string Sanitize(string? value) =>
        value is null ? string.Empty : TabOrNewline().Replace(value, " ");

    public static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return Whitespace().Replace(name, " ").Trim();
    }

    [GeneratedRegex(@"\t|\r|\n")]
    private static partial Regex TabOrNewline();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
