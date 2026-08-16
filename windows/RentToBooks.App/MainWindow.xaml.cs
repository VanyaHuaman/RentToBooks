using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RentToBooks.App.Resources;
using RentToBooks.Core;
using Wpf.Ui.Controls;

namespace RentToBooks.App;

public partial class MainWindow : FluentWindow
{
    private const string ExplorerExecutable = "explorer.exe";
    private const string NotepadExecutable = "notepad.exe";
    private const string ProcessingDateFormat = "MMddyyyy";

    private readonly AppSettingsStore _settingsStore = new();
    private AppSettings _settings;
    private IReadOnlyList<string> _lastOutputPaths = [];

    public MainWindow()
    {
        InitializeComponent();
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        ProcessTypeBox.ItemsSource = ProcessTypeOption.All;

        _settings = _settingsStore.Load();
        OutputPathBox.Text = _settings.LastOutputDirectory;
        ReceivableAccountBox.Text = _settings.ReceivableAccount;
        DepositAccountBox.Text = _settings.DepositAccount;
        IncomeAccountBox.Text = _settings.IncomeAccount;
        ProcessTypeBox.SelectedItem = ProcessTypeOption.All.First(option =>
            option.Value == (_settings.ProcessType is ProcessType.Invoice or ProcessType.Both
                ? _settings.ProcessType
                : ProcessType.Payment));

        UpdateProcessAccountFields();
    }

    private ProcessType SelectedProcessType => ((ProcessTypeOption)ProcessTypeBox.SelectedItem).Value;

    private void ProcessTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateProcessAccountFields();

    private void UpdateProcessAccountFields()
    {
        if (ProcessTypeBox.SelectedItem is not ProcessTypeOption selected)
        {
            return;
        }

        var isInvoice = selected.Value == ProcessType.Invoice;
        var isBoth = selected.Value == ProcessType.Both;

        DepositLabel.IsEnabled = !isInvoice || isBoth;
        DepositAccountBox.IsEnabled = !isInvoice || isBoth;
        IncomeLabel.IsEnabled = isInvoice || isBoth;
        IncomeAccountBox.IsEnabled = isInvoice || isBoth;

        if (string.IsNullOrWhiteSpace(ReceivableAccountBox.Text))
        {
            ReceivableAccountBox.Text = DefaultAccounts.ReceivableAccount;
        }

        if ((isInvoice || isBoth) && string.IsNullOrWhiteSpace(IncomeAccountBox.Text))
        {
            IncomeAccountBox.Text = DefaultAccounts.IncomeAccount;
        }

        if ((!isInvoice || isBoth) && string.IsNullOrWhiteSpace(DepositAccountBox.Text))
        {
            DepositAccountBox.Text = DefaultAccounts.DepositAccount;
        }

        PreviewGrid.ItemsSource = null;
        OpenIifButton.IsEnabled = false;
        CopyPathButton.IsEnabled = false;
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = AppStrings.ChooseReportDialogTitle,
            Filter = AppStrings.ExcelFileFilter,
            CheckFileExists = true,
        };
        if (!string.IsNullOrWhiteSpace(_settings.LastInputDirectory) && Directory.Exists(_settings.LastInputDirectory))
        {
            dialog.InitialDirectory = _settings.LastInputDirectory;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        InputPathBox.Text = dialog.FileName;
        ProcessingDateBox.Text = RentReportConverter.GetDateFromFileName(dialog.FileName);

        try
        {
            var detected = RentReportConverter.DetectProcessType(dialog.FileName);
            if (detected.ProcessType is { } processType)
            {
                ProcessTypeBox.SelectedItem = ProcessTypeOption.All.First(option => option.Value == processType);
                var displayText = ProcessTypeOption.All.First(option => option.Value == processType).DisplayText;
                StatusText.Text = string.Format(
                    AppStrings.DetectedProcessType, displayText, detected.InvoiceRows, detected.PaymentRows);
            }
            else
            {
                StatusText.Text = AppStrings.CouldNotDetectProcessType;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = AppStrings.CouldNotAutoDetectProcessType;
            LogBox.Text = ex.Message;
        }

        // Input and output folder are independent: browsing input never touches OutputPathBox.
        PreviewGrid.ItemsSource = null;
        OpenIifButton.IsEnabled = false;
        CopyPathButton.IsEnabled = false;
        SaveSettings();
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = AppStrings.ChooseOutputFolderDialogTitle };
        if (!string.IsNullOrWhiteSpace(OutputPathBox.Text) && Directory.Exists(OutputPathBox.Text))
        {
            dialog.InitialDirectory = OutputPathBox.Text;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        OutputPathBox.Text = dialog.FolderName;
        SaveSettings();
    }

    private sealed record FormInput(
        string InputPath,
        string OutputDirectory,
        ProcessType ProcessType,
        DateTime ProcessingDate,
        string ReceivableAccount,
        string DepositAccount,
        string IncomeAccount);

    private FormInput GetFormInput()
    {
        if (string.IsNullOrWhiteSpace(InputPathBox.Text))
        {
            throw new InvalidOperationException(AppStrings.ChooseReportFirst);
        }

        if (string.IsNullOrWhiteSpace(OutputPathBox.Text))
        {
            throw new InvalidOperationException(AppStrings.ChooseOutputFolderFirst);
        }

        return new FormInput(
            InputPathBox.Text,
            OutputPathBox.Text,
            SelectedProcessType,
            GetValidatedDate(ProcessingDateBox.Text),
            ReceivableAccountBox.Text,
            DepositAccountBox.Text,
            IncomeAccountBox.Text);
    }

    private static DateTime GetValidatedDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(AppStrings.EnterProcessingDate);
        }

        if (!DateTime.TryParseExact(
                text, ProcessingDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new InvalidOperationException(AppStrings.InvalidProcessingDate);
        }

        return date;
    }

    private void SaveSettings(FormInput? input = null)
    {
        _settingsStore.Save(
            input?.InputPath ?? InputPathBox.Text,
            input?.OutputDirectory ?? OutputPathBox.Text,
            input?.ProcessType ?? SelectedProcessType,
            input?.ReceivableAccount ?? ReceivableAccountBox.Text,
            input?.DepositAccount ?? DepositAccountBox.Text,
            input?.IncomeAccount ?? IncomeAccountBox.Text);
        _settings = _settingsStore.Load();
    }

    private async void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        PreviewButton.IsEnabled = false;
        StatusText.Text = AppStrings.BuildingPreview;
        LogBox.Text = "";

        try
        {
            var input = GetFormInput();
            var data = await Task.Run(() => RentReportConverter.BuildPreviewData(
                input.InputPath, input.ProcessingDate, input.ProcessType,
                input.ReceivableAccount, input.DepositAccount, input.IncomeAccount));

            PreviewGrid.ItemsSource = data.PreviewRows;
            StatusText.Text = AppStrings.PreviewReady;
            LogBox.Text = string.Join(
                Environment.NewLine,
                string.Format(AppStrings.PreviewRowsSummary, data.ProcessedRows),
                string.Format(AppStrings.SkippedRowsSummary, data.SkippedRows),
                AppStrings.ReviewRowsHint);
            SaveSettings(input);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppStrings.CouldNotBuildPreview;
            PreviewGrid.ItemsSource = null;
            LogBox.Text = ex.Message;
        }
        finally
        {
            PreviewButton.IsEnabled = true;
        }
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        CreateButton.IsEnabled = false;
        PreviewButton.IsEnabled = false;
        OpenFolderButton.IsEnabled = false;
        OpenIifButton.IsEnabled = false;
        CopyPathButton.IsEnabled = false;
        StatusText.Text = AppStrings.CreatingIifFile;
        LogBox.Text = "";

        try
        {
            var input = GetFormInput();
            var existingOutputPaths = RentReportConverter
                .GetOutputPaths(input.OutputDirectory, input.ProcessingDate, input.ProcessType)
                .Where(File.Exists)
                .ToList();

            if (existingOutputPaths.Count > 0)
            {
                var message = string.Join(
                    Environment.NewLine,
                    AppStrings.ReplaceFileMessageIntro,
                    "",
                    string.Join(Environment.NewLine, existingOutputPaths),
                    "",
                    AppStrings.ContinuePrompt);

                var choice = System.Windows.MessageBox.Show(
                    message,
                    AppStrings.ReplaceFileDialogTitle,
                    System.Windows.MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (choice != System.Windows.MessageBoxResult.Yes)
                {
                    StatusText.Text = AppStrings.CreateIifCanceled;
                    LogBox.Text = AppStrings.NoFilesChanged;
                    return;
                }
            }

            var result = await Task.Run(() => RentReportConverter.ConvertToIif(
                input.InputPath, input.OutputDirectory, input.ProcessingDate, input.ProcessType,
                input.ReceivableAccount, input.DepositAccount, input.IncomeAccount));

            _lastOutputPaths = result.OutputPaths;
            PreviewGrid.ItemsSource = result.PreviewRows;

            var processTypeDisplay = ProcessTypeOption.All.First(option => option.Value == input.ProcessType).DisplayText;
            StatusText.Text = AppStrings.IifFileCreated;
            LogBox.Text = string.Join(
                Environment.NewLine,
                AppStrings.CreatedIifLog,
                string.Format(AppStrings.OutputLog, string.Join(Environment.NewLine, result.OutputPaths)),
                string.Format(AppStrings.ProcessedRowsLog, processTypeDisplay, result.ProcessedRows),
                string.Format(AppStrings.SkippedRowsSummary, result.SkippedRows),
                "",
                AppStrings.IifFormatNote);
            OpenFolderButton.IsEnabled = true;
            OpenIifButton.IsEnabled = _lastOutputPaths.Count == 1;
            CopyPathButton.IsEnabled = true;
            SaveSettings(input);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppStrings.CouldNotCreateIifFile;
            LogBox.Text = ex.Message;
        }
        finally
        {
            CreateButton.IsEnabled = true;
            PreviewButton.IsEnabled = true;
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOutputPaths.Count == 0)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_lastOutputPaths[0]);
        if (directory is null || !Directory.Exists(directory))
        {
            return;
        }

        var psi = new ProcessStartInfo(ExplorerExecutable) { UseShellExecute = true };
        psi.ArgumentList.Add(directory);
        Process.Start(psi);
    }

    private void OpenIifButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOutputPaths.Count != 1 || !File.Exists(_lastOutputPaths[0]))
        {
            return;
        }

        var psi = new ProcessStartInfo(NotepadExecutable) { UseShellExecute = true };
        psi.ArgumentList.Add(_lastOutputPaths[0]);
        Process.Start(psi);
    }

    private void CopyPathButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOutputPaths.Count == 0)
        {
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine, _lastOutputPaths));
        StatusText.Text = AppStrings.OutputPathCopied;
    }
}
