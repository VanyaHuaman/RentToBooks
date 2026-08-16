namespace RentToBooks.Core;

/// <summary>Builds IIF lines and preview rows for a single Payment or Invoice pass.</summary>
public static class RentIifDataBuilder
{
    private const int TenantColumn = 3;
    private const int DatetimeColumn = 6;
    private const int InvoicedColumn = 7;
    private const int PaymentColumn = 9;

    public static RentIifData Build(
        string inputPath,
        DateTime processingDate,
        ProcessType processType,
        string receivableAccount,
        string depositAccount,
        string incomeAccount)
    {
        if (processType == ProcessType.Both)
        {
            throw new ArgumentException("Build only supports Payment or Invoice, not Both.", nameof(processType));
        }

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file was not found: {inputPath}", inputPath);
        }

        if (string.IsNullOrWhiteSpace(receivableAccount))
        {
            throw new InvalidOperationException("Enter the QuickBooks accounts receivable account name.");
        }

        if (string.IsNullOrWhiteSpace(depositAccount))
        {
            throw new InvalidOperationException("Enter the QuickBooks deposit account name.");
        }

        if (string.IsNullOrWhiteSpace(incomeAccount))
        {
            throw new InvalidOperationException("Enter the QuickBooks income account name.");
        }

        var rows = XlsxReportReader.ReadFirstSheetRows(inputPath);
        RentReportHeaderValidator.Assert(rows);

        var isInvoice = processType == ProcessType.Invoice;
        var transactionType = isInvoice ? "INVOICE" : "PAYMENT";
        var amountColumn = isInvoice ? InvoicedColumn : PaymentColumn;
        var trnsAccount = isInvoice ? receivableAccount : depositAccount;
        var splAccount = isInvoice ? incomeAccount : receivableAccount;
        var modeLabel = isInvoice ? "invoiced amounts in column G" : "payment amounts in column I";

        var iifLines = new List<string>
        {
            IifLine.Build("!TRNS", "TRNSID", "TRNSTYPE", "DATE", "ACCNT", "NAME", "AMOUNT", "DOCNUM"),
            IifLine.Build("!SPL", "SPLID", "TRNSTYPE", "DATE", "ACCNT", "NAME", "AMOUNT", "DOCNUM"),
            IifLine.Build("!ENDTRNS", "", "", "", "", "", "", ""),
        };
        var previewRows = new List<RentPreviewRow>();

        var processedRows = 0;
        var skippedRows = 0;

        foreach (var row in rows.Where(r => r.RowNumber > 1))
        {
            var tenantValue = TextFormatting.NormalizeName(row.GetText(TenantColumn));
            if (string.IsNullOrWhiteSpace(tenantValue))
            {
                skippedRows++;
                continue;
            }

            var currentAmount = AmountParsing.ParseIifAmount(row.GetText(amountColumn));
            if (currentAmount is null or 0m)
            {
                skippedRows++;
                continue;
            }

            var dateTimeValue = row.GetText(DatetimeColumn);
            var iifDate = DateFormatting.ToIifDate(dateTimeValue, processingDate);
            const string docNum = "";
            var amount = Math.Abs(currentAmount.Value);
            var splitAmount = -amount;
            var amountText = AmountParsing.FormatIifAmount(amount);
            var splitAmountText = AmountParsing.FormatIifAmount(splitAmount);

            previewRows.Add(new RentPreviewRow(
                processType,
                row.RowNumber,
                tenantValue,
                iifDate,
                amountText,
                trnsAccount,
                splAccount,
                docNum,
                dateTimeValue));

            iifLines.Add(IifLine.Build("TRNS", " ", transactionType, iifDate, trnsAccount, tenantValue, amountText, docNum));
            iifLines.Add(IifLine.Build("SPL", " ", transactionType, iifDate, splAccount, tenantValue, splitAmountText, docNum));
            iifLines.Add(IifLine.Build("ENDTRNS", "", "", "", "", "", "", ""));
            processedRows++;
        }

        if (processedRows == 0)
        {
            throw new InvalidOperationException(
                $"No {processType.ToString().ToLowerInvariant()} rows were found. Check that the selected workbook has {modeLabel}.");
        }

        return new RentIifData(iifLines, previewRows, processedRows, skippedRows);
    }
}
