using RentToBooks.Core.Resources;

namespace RentToBooks.Core;

/// <summary>Builds IIF lines and preview rows for a single Payment or Invoice pass.</summary>
public static class RentIifDataBuilder
{
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
            throw new ArgumentException(CoreMessages.BuildOnlySupportsSingleType, nameof(processType));
        }

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException(
                string.Format(CoreMessages.InputFileNotFound, inputPath), inputPath);
        }

        if (string.IsNullOrWhiteSpace(receivableAccount))
        {
            throw new InvalidOperationException(CoreMessages.MissingReceivableAccount);
        }

        if (string.IsNullOrWhiteSpace(depositAccount))
        {
            throw new InvalidOperationException(CoreMessages.MissingDepositAccount);
        }

        if (string.IsNullOrWhiteSpace(incomeAccount))
        {
            throw new InvalidOperationException(CoreMessages.MissingIncomeAccount);
        }

        var rows = XlsxReportReader.ReadFirstSheetRows(inputPath);
        RentReportHeaderValidator.Assert(rows);

        var isInvoice = processType == ProcessType.Invoice;
        var transactionType = isInvoice ? IifFormat.InvoiceTransactionType : IifFormat.PaymentTransactionType;
        var amountColumn = isInvoice ? ReportColumns.Invoiced : ReportColumns.Payment;
        var trnsAccount = isInvoice ? receivableAccount : depositAccount;
        var splAccount = isInvoice ? incomeAccount : receivableAccount;
        var modeLabel = isInvoice ? CoreMessages.InvoiceModeLabel : CoreMessages.PaymentModeLabel;

        var iifLines = new List<string>
        {
            IifLine.Build(
                IifFormat.TrnsHeaderTag, IifFormat.TrnsIdField, IifFormat.TrnsTypeField, IifFormat.DateField,
                IifFormat.AccountField, IifFormat.NameField, IifFormat.AmountField, IifFormat.DocNumField),
            IifLine.Build(
                IifFormat.SplHeaderTag, IifFormat.SplIdField, IifFormat.TrnsTypeField, IifFormat.DateField,
                IifFormat.AccountField, IifFormat.NameField, IifFormat.AmountField, IifFormat.DocNumField),
            IifLine.Build(IifFormat.EndTrnsHeaderTag, "", "", "", "", "", "", ""),
        };
        var previewRows = new List<RentPreviewRow>();

        var processedRows = 0;
        var skippedRows = 0;

        foreach (var row in rows.Where(r => r.RowNumber > 1))
        {
            var tenantValue = TextFormatting.NormalizeName(row.GetText(ReportColumns.Tenant));
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

            var dateTimeValue = row.GetText(ReportColumns.Datetime);
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

            iifLines.Add(IifLine.Build(
                IifFormat.TrnsRowTag, " ", transactionType, iifDate, trnsAccount, tenantValue, amountText, docNum));
            iifLines.Add(IifLine.Build(
                IifFormat.SplRowTag, " ", transactionType, iifDate, splAccount, tenantValue, splitAmountText, docNum));
            iifLines.Add(IifLine.Build(IifFormat.EndTrnsRowTag, "", "", "", "", "", "", ""));
            processedRows++;
        }

        if (processedRows == 0)
        {
            throw new InvalidOperationException(
                string.Format(CoreMessages.NoRowsFound, processType.ToString().ToLowerInvariant(), modeLabel));
        }

        return new RentIifData(iifLines, previewRows, processedRows, skippedRows);
    }
}
