using System.Globalization;
using System.Reflection;
using System.Resources;

namespace RentToBooks.Core.Resources;

/// <summary>
/// Strongly-typed accessor for CoreMessages.resx. Hand-written rather than IDE-generated
/// (this repo is built via the dotnet CLI), but follows the same pattern .NET's own
/// ResXFileCodeGenerator produces: one static string property per resource key, resolved
/// against the current UI culture so satellite resource assemblies (e.g. CoreMessages.es.resx)
/// can be dropped in later with no code changes.
/// </summary>
public static class CoreMessages
{
    private static readonly ResourceManager ResourceManager = new(
        "RentToBooks.Core.Resources.CoreMessages", Assembly.GetExecutingAssembly());

    public static string InputFileNotFound => Get(nameof(InputFileNotFound));
    public static string MissingReceivableAccount => Get(nameof(MissingReceivableAccount));
    public static string MissingDepositAccount => Get(nameof(MissingDepositAccount));
    public static string MissingIncomeAccount => Get(nameof(MissingIncomeAccount));
    public static string NoTransactionRows => Get(nameof(NoTransactionRows));
    public static string HeaderValidationFailed => Get(nameof(HeaderValidationFailed));
    public static string HeaderColumnMismatch => Get(nameof(HeaderColumnMismatch));
    public static string NoRowsFound => Get(nameof(NoRowsFound));
    public static string InvoiceModeLabel => Get(nameof(InvoiceModeLabel));
    public static string PaymentModeLabel => Get(nameof(PaymentModeLabel));
    public static string BuildOnlySupportsSingleType => Get(nameof(BuildOnlySupportsSingleType));

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
