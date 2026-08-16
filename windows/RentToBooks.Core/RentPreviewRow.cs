namespace RentToBooks.Core;

public sealed record RentPreviewRow(
    ProcessType Type,
    int SourceRow,
    string Tenant,
    string Date,
    string Amount,
    string TrnsAccount,
    string SplAccount,
    string DocNum,
    string SourceDateTime);
