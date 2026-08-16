using RentToBooks.Core.Resources;

namespace RentToBooks.Core;

public static class RentReportHeaderValidator
{
    private const int MinimumRowCount = 2; // header row + at least one data row

    // Column -> expected header text (a contract with the source workbook, not user-facing text).
    private static readonly IReadOnlyDictionary<int, string> Expected = new Dictionary<int, string>
    {
        [ReportColumns.Tenant] = ReportColumns.TenantHeader,
        [ReportColumns.Datetime] = ReportColumns.DatetimeHeader,
        [ReportColumns.Invoiced] = ReportColumns.InvoicedHeader,
        [ReportColumns.Payment] = ReportColumns.PaymentHeader,
    };

    public static void Assert(IReadOnlyList<XlsxRow> rows)
    {
        if (rows.Count < MinimumRowCount)
        {
            throw new InvalidOperationException(CoreMessages.NoTransactionRows);
        }

        var header = rows[0];
        var missing = new List<string>();
        foreach (var (column, expectedText) in Expected)
        {
            var actual = header.GetText(column);
            if (actual != expectedText)
            {
                missing.Add(string.Format(CoreMessages.HeaderColumnMismatch, column, expectedText, actual));
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                CoreMessages.HeaderValidationFailed + Environment.NewLine + string.Join(Environment.NewLine, missing));
        }
    }
}
