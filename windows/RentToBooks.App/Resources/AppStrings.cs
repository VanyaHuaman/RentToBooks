using System.Globalization;
using System.Reflection;
using System.Resources;

namespace RentToBooks.App.Resources;

/// <summary>
/// Strongly-typed accessor for AppStrings.resx, following the same shape .NET's
/// ResXFileCodeGenerator produces (hand-written since this repo is built via the dotnet
/// CLI). Every string a user reads lives here so a future satellite resource file
/// (e.g. AppStrings.es.resx) is the only thing needed to add another language.
/// </summary>
public static class AppStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "RentToBooks.App.Resources.AppStrings", Assembly.GetExecutingAssembly());

    public static string WindowTitle => Get(nameof(WindowTitle));
    public static string HeadingText => Get(nameof(HeadingText));
    public static string InitialStatus => Get(nameof(InitialStatus));

    public static string RentReportLabel => Get(nameof(RentReportLabel));
    public static string DateLabel => Get(nameof(DateLabel));
    public static string DateHint => Get(nameof(DateHint));
    public static string ProcessLabel => Get(nameof(ProcessLabel));
    public static string OutputFolderLabel => Get(nameof(OutputFolderLabel));
    public static string ReceivableAccountLabel => Get(nameof(ReceivableAccountLabel));
    public static string DepositAccountLabel => Get(nameof(DepositAccountLabel));
    public static string IncomeAccountLabel => Get(nameof(IncomeAccountLabel));

    public static string BrowseButton => Get(nameof(BrowseButton));
    public static string PreviewButton => Get(nameof(PreviewButton));
    public static string CreateIifButton => Get(nameof(CreateIifButton));
    public static string OpenFolderButton => Get(nameof(OpenFolderButton));
    public static string OpenIifButton => Get(nameof(OpenIifButton));
    public static string CopyPathButton => Get(nameof(CopyPathButton));

    public static string ProcessTypePayment => Get(nameof(ProcessTypePayment));
    public static string ProcessTypeInvoice => Get(nameof(ProcessTypeInvoice));
    public static string ProcessTypeBoth => Get(nameof(ProcessTypeBoth));

    public static string ChooseReportDialogTitle => Get(nameof(ChooseReportDialogTitle));
    public static string ExcelFileFilter => Get(nameof(ExcelFileFilter));
    public static string ChooseOutputFolderDialogTitle => Get(nameof(ChooseOutputFolderDialogTitle));
    public static string ReplaceFileDialogTitle => Get(nameof(ReplaceFileDialogTitle));
    public static string ReplaceFileMessageIntro => Get(nameof(ReplaceFileMessageIntro));
    public static string ContinuePrompt => Get(nameof(ContinuePrompt));

    public static string DetectedProcessType => Get(nameof(DetectedProcessType));
    public static string CouldNotDetectProcessType => Get(nameof(CouldNotDetectProcessType));
    public static string CouldNotAutoDetectProcessType => Get(nameof(CouldNotAutoDetectProcessType));
    public static string BuildingPreview => Get(nameof(BuildingPreview));
    public static string PreviewReady => Get(nameof(PreviewReady));
    public static string PreviewRowsSummary => Get(nameof(PreviewRowsSummary));
    public static string SkippedRowsSummary => Get(nameof(SkippedRowsSummary));
    public static string ReviewRowsHint => Get(nameof(ReviewRowsHint));
    public static string CouldNotBuildPreview => Get(nameof(CouldNotBuildPreview));
    public static string CreatingIifFile => Get(nameof(CreatingIifFile));
    public static string CreateIifCanceled => Get(nameof(CreateIifCanceled));
    public static string NoFilesChanged => Get(nameof(NoFilesChanged));
    public static string IifFileCreated => Get(nameof(IifFileCreated));
    public static string CreatedIifLog => Get(nameof(CreatedIifLog));
    public static string OutputLog => Get(nameof(OutputLog));
    public static string ProcessedRowsLog => Get(nameof(ProcessedRowsLog));
    public static string IifFormatNote => Get(nameof(IifFormatNote));
    public static string CouldNotCreateIifFile => Get(nameof(CouldNotCreateIifFile));
    public static string OutputPathCopied => Get(nameof(OutputPathCopied));
    public static string ChooseReportFirst => Get(nameof(ChooseReportFirst));
    public static string ChooseOutputFolderFirst => Get(nameof(ChooseOutputFolderFirst));
    public static string EnterProcessingDate => Get(nameof(EnterProcessingDate));
    public static string InvalidProcessingDate => Get(nameof(InvalidProcessingDate));

    public static string UpdateAvailable => Get(nameof(UpdateAvailable));

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
