using System.Globalization;

namespace RentToBooks.Core;

public static class AmountParsing
{
    /// <summary>
    /// Parses accounting-style amount text: strips "$" and ",", and treats
    /// "(123.45)" as -123.45. Returns null when the value is blank or unparseable.
    /// </summary>
    public static decimal? ParseIifAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var isParenthesizedNegative = text.StartsWith('(') && text.EndsWith(')');
        text = text.Trim('(', ')').Replace("$", string.Empty).Replace(",", string.Empty);

        if (!decimal.TryParse(
                text,
                NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        return isParenthesizedNegative ? -amount : amount;
    }

    public static string FormatIifAmount(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);
}
