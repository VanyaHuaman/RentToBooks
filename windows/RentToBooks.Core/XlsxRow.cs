namespace RentToBooks.Core;

public sealed record XlsxRow(int RowNumber, IReadOnlyDictionary<int, string> Values)
{
    public string GetText(int column) => Values.TryGetValue(column, out var value) ? value : string.Empty;
}
