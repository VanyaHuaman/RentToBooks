using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RentToBooks.Core;

public static partial class RentReportConverter
{
    private const int TenantColumn = 3;
    private const int InvoicedColumn = 7;
    private const int PaymentColumn = 9;
    private const string ProcessingDateFileFormat = "MMddyyyy";

    public static RentConversionResult ConvertToIif(
        string inputPath,
        string outputDirectory,
        DateTime processingDate,
        ProcessType processType,
        string receivableAccount,
        string depositAccount,
        string incomeAccount)
    {
        Directory.CreateDirectory(outputDirectory);

        if (processType == ProcessType.Both)
        {
            var invoiceResult = ConvertToIif(
                inputPath, outputDirectory, processingDate, ProcessType.Invoice,
                receivableAccount, depositAccount, incomeAccount);
            var paymentResult = ConvertToIif(
                inputPath, outputDirectory, processingDate, ProcessType.Payment,
                receivableAccount, depositAccount, incomeAccount);

            return new RentConversionResult(
                [.. invoiceResult.OutputPaths, .. paymentResult.OutputPaths],
                invoiceResult.ProcessedRows + paymentResult.ProcessedRows,
                invoiceResult.SkippedRows + paymentResult.SkippedRows,
                [.. invoiceResult.PreviewRows, .. paymentResult.PreviewRows]);
        }

        var outputPath = Path.Combine(outputDirectory, BuildOutputFileName(processType, processingDate));
        var data = RentIifDataBuilder.Build(
            inputPath, processingDate, processType, receivableAccount, depositAccount, incomeAccount);

        File.WriteAllLines(outputPath, data.IifLines, new UTF8Encoding(false));

        return new RentConversionResult([outputPath], data.ProcessedRows, data.SkippedRows, data.PreviewRows);
    }

    public static IReadOnlyList<string> GetOutputPaths(
        string outputDirectory, DateTime processingDate, ProcessType processType)
    {
        var types = processType == ProcessType.Both
            ? [ProcessType.Invoice, ProcessType.Payment]
            : (ProcessType[])[processType];

        return types
            .Select(type => Path.Combine(outputDirectory, BuildOutputFileName(type, processingDate)))
            .ToList();
    }

    public static RentPreviewData BuildPreviewData(
        string inputPath,
        DateTime processingDate,
        ProcessType processType,
        string receivableAccount,
        string depositAccount,
        string incomeAccount)
    {
        if (processType == ProcessType.Both)
        {
            var invoiceData = RentIifDataBuilder.Build(
                inputPath, processingDate, ProcessType.Invoice, receivableAccount, depositAccount, incomeAccount);
            var paymentData = RentIifDataBuilder.Build(
                inputPath, processingDate, ProcessType.Payment, receivableAccount, depositAccount, incomeAccount);

            return new RentPreviewData(
                [.. invoiceData.PreviewRows, .. paymentData.PreviewRows],
                invoiceData.ProcessedRows + paymentData.ProcessedRows,
                invoiceData.SkippedRows + paymentData.SkippedRows);
        }

        var data = RentIifDataBuilder.Build(
            inputPath, processingDate, processType, receivableAccount, depositAccount, incomeAccount);
        return new RentPreviewData(data.PreviewRows, data.ProcessedRows, data.SkippedRows);
    }

    public static RentProcessTypeDetection DetectProcessType(string inputPath)
    {
        var rows = XlsxReportReader.ReadFirstSheetRows(inputPath);
        RentReportHeaderValidator.Assert(rows);

        var invoiceRows = 0;
        var paymentRows = 0;

        foreach (var row in rows.Where(r => r.RowNumber > 1))
        {
            var tenantValue = TextFormatting.NormalizeName(row.GetText(TenantColumn));
            if (string.IsNullOrWhiteSpace(tenantValue))
            {
                continue;
            }

            var invoiceAmount = AmountParsing.ParseIifAmount(row.GetText(InvoicedColumn));
            var paymentAmount = AmountParsing.ParseIifAmount(row.GetText(PaymentColumn));

            if (invoiceAmount is not null and not 0m)
            {
                invoiceRows++;
            }

            if (paymentAmount is not null and not 0m)
            {
                paymentRows++;
            }
        }

        ProcessType? detected = (invoiceRows > 0, paymentRows > 0) switch
        {
            (true, true) => ProcessType.Both,
            (true, false) => ProcessType.Invoice,
            (false, true) => ProcessType.Payment,
            _ => null,
        };

        return new RentProcessTypeDetection(detected, invoiceRows, paymentRows);
    }

    public static string GetDateFromFileName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var match = EightDigits().Match(name);
        if (!match.Success)
        {
            return string.Empty;
        }

        return DateTime.TryParseExact(
            match.Groups[1].Value,
            ProcessingDateFileFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date.ToString(ProcessingDateFileFormat, CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string BuildOutputFileName(ProcessType processType, DateTime processingDate)
    {
        var prefix = processType == ProcessType.Invoice ? "RentInvoice" : "RentPayment";
        var dateText = processingDate.ToString(ProcessingDateFileFormat, CultureInfo.InvariantCulture);
        return $"{prefix}{dateText}.iif";
    }

    [GeneratedRegex(@"(\d{8})")]
    private static partial Regex EightDigits();
}
