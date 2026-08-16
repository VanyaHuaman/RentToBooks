namespace RentToBooks.Core;

public static class IifLine
{
    public static string Build(params string?[] fields) =>
        string.Join('\t', fields.Select(TextFormatting.Sanitize));
}
