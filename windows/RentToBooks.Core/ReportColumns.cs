namespace RentToBooks.Core;

/// <summary>
/// Column layout of the rent transaction report. These are contract values dictated by
/// the source workbook's format, not user-facing text — they must never be localized.
/// </summary>
public static class ReportColumns
{
    public const int Tenant = 3;
    public const int Datetime = 6;
    public const int Invoiced = 7;
    public const int Payment = 9;

    public const string TenantHeader = "Tenant";
    public const string DatetimeHeader = "Datetime";
    public const string InvoicedHeader = "Invoiced";
    public const string PaymentHeader = "Payment";
}
