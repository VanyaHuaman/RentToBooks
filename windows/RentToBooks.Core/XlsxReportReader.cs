using System.Globalization;
using ClosedXML.Excel;

namespace RentToBooks.Core;

public static class XlsxReportReader
{
    public static IReadOnlyList<XlsxRow> ReadFirstSheetRows(string path)
    {
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<XlsxRow>();
        foreach (var row in worksheet.RowsUsed())
        {
            var values = new Dictionary<int, string>();
            foreach (var cell in row.CellsUsed())
            {
                values[cell.Address.ColumnNumber] = GetCellText(cell);
            }

            rows.Add(new XlsxRow(row.RowNumber(), values));
        }

        return rows;
    }

    private static string GetCellText(IXLCell cell)
    {
        var value = cell.Value;

        var raw = value.Type switch
        {
            XLDataType.Blank => string.Empty,
            XLDataType.Number => value.GetNumber().ToString(CultureInfo.InvariantCulture),
            // Excel stores dates as a numeric serial internally; mirror that raw value
            // rather than a formatted date string, matching the source report's shape.
            XLDataType.DateTime => value.GetDateTime().ToOADate().ToString(CultureInfo.InvariantCulture),
            XLDataType.Text => value.GetText(),
            _ => value.ToString() ?? string.Empty,
        };

        return TextFormatting.Sanitize(raw);
    }
}
