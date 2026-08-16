using System.Globalization;

namespace RentToBooks.Core;

public static class DateFormatting
{
    private const string IifDateFormat = "M/d/yyyy";

    /// <summary>
    /// Formats a report cell's date text for an IIF line, falling back to
    /// <paramref name="fallbackDate"/> when the value is blank or unparseable.
    /// </summary>
    public static string ToIifDate(string? value, DateTime fallbackDate)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackDate.ToString(IifDateFormat, CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
        {
            return date.ToString(IifDateFormat, CultureInfo.InvariantCulture);
        }

        return fallbackDate.ToString(IifDateFormat, CultureInfo.InvariantCulture);
    }
}
