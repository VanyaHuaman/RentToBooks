namespace RentToBooks.Core;

public sealed record RentConversionResult(
    IReadOnlyList<string> OutputPaths,
    int ProcessedRows,
    int SkippedRows,
    IReadOnlyList<RentPreviewRow> PreviewRows);

public sealed record RentPreviewData(
    IReadOnlyList<RentPreviewRow> PreviewRows,
    int ProcessedRows,
    int SkippedRows);

public sealed record RentProcessTypeDetection(
    ProcessType? ProcessType,
    int InvoiceRows,
    int PaymentRows);
