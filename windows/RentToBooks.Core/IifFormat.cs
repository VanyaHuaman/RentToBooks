namespace RentToBooks.Core;

/// <summary>
/// QuickBooks .iif file format keywords. These are a fixed wire format dictated by
/// QuickBooks, not user-facing text — they must never be localized.
/// </summary>
public static class IifFormat
{
    public const string TrnsHeaderTag = "!TRNS";
    public const string SplHeaderTag = "!SPL";
    public const string EndTrnsHeaderTag = "!ENDTRNS";

    public const string TrnsRowTag = "TRNS";
    public const string SplRowTag = "SPL";
    public const string EndTrnsRowTag = "ENDTRNS";

    public const string TrnsIdField = "TRNSID";
    public const string SplIdField = "SPLID";
    public const string TrnsTypeField = "TRNSTYPE";
    public const string DateField = "DATE";
    public const string AccountField = "ACCNT";
    public const string NameField = "NAME";
    public const string AmountField = "AMOUNT";
    public const string DocNumField = "DOCNUM";

    public const string InvoiceTransactionType = "INVOICE";
    public const string PaymentTransactionType = "PAYMENT";
}
