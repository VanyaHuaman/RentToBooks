namespace RentToBooks.Core;

public static class RentReportHeaderValidator
{
    // Column -> expected header text.
    private static readonly IReadOnlyDictionary<int, string> Expected = new Dictionary<int, string>
    {
        [3] = "Tenant",
        [6] = "Datetime",
        [7] = "Invoiced",
        [9] = "Payment",
    };

    public static void Assert(IReadOnlyList<XlsxRow> rows)
    {
        if (rows.Count < 2)
        {
            throw new InvalidOperationException("The selected workbook does not contain rent transaction rows.");
        }

        var header = rows[0];
        var missing = new List<string>();
        foreach (var (column, expectedText) in Expected)
        {
            var actual = header.GetText(column);
            if (actual != expectedText)
            {
                missing.Add($"Column {column} expected '{expectedText}' but found '{actual}'");
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "The selected workbook does not look like the expected rent transaction report." +
                Environment.NewLine + string.Join(Environment.NewLine, missing));
        }
    }
}
