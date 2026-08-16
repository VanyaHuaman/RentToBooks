namespace RentToBooks.Core;

public sealed record RentIifData(
    IReadOnlyList<string> IifLines,
    IReadOnlyList<RentPreviewRow> PreviewRows,
    int ProcessedRows,
    int SkippedRows);
