using System.Globalization;
using RentToBooks.Core;

namespace RentToBooks.Core.Tests;

/// <summary>
/// Byte-for-byte regression tests against fixtures carried over from RentIifConverter,
/// confirming this port produces identical .iif output to the tool it replaces.
/// </summary>
public class RentReportConverterRegressionTests
{
    private static readonly string FixturesRoot = Path.Combine(AppContext.BaseDirectory, "fixtures");
    private static readonly string InputDir = Path.Combine(FixturesRoot, "input");
    private static readonly string ExpectedDir = Path.Combine(FixturesRoot, "expected");

    private const string ReceivableAccount = "A11000 - Accounts Receivable";
    private const string DepositAccount = "A12000 - Undeposited Funds";
    private const string IncomeAccount = "A47600 - ARB Rental Income";

    [Fact]
    public void PaymentReport_MatchesExpectedIif() =>
        RunCase(ProcessType.Payment, "payment-06122026.xlsx", "06122026", ["RentPayment06122026.iif"]);

    [Fact]
    public void InvoiceReport_MatchesExpectedIif() =>
        RunCase(ProcessType.Invoice, "invoice-06112026.xlsx", "06112026", ["RentInvoice06112026.iif"]);

    [Fact]
    public void MixedReport_MatchesExpectedIifFiles() =>
        RunCase(
            ProcessType.Both,
            "mixed-06262026.xlsx",
            "06262026",
            ["RentInvoice06262026.iif", "RentPayment06262026.iif"]);

    private static void RunCase(
        ProcessType processType, string inputFile, string processingDateText, string[] expectedFiles)
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "RentToBooksTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir);

        try
        {
            var processingDate = DateTime.ParseExact(
                processingDateText, "MMddyyyy", CultureInfo.InvariantCulture);

            RentReportConverter.ConvertToIif(
                Path.Combine(InputDir, inputFile),
                outputDir,
                processingDate,
                processType,
                ReceivableAccount,
                DepositAccount,
                IncomeAccount);

            foreach (var file in expectedFiles)
            {
                var actualPath = Path.Combine(outputDir, file);
                Assert.True(File.Exists(actualPath), $"Expected output file was not created: {file}");

                var expectedBytes = File.ReadAllBytes(Path.Combine(ExpectedDir, file));
                var actualBytes = File.ReadAllBytes(actualPath);
                Assert.Equal(expectedBytes, actualBytes);
            }
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }
}
