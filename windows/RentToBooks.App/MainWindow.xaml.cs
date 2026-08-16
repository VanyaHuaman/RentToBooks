using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RentToBooks.Core;
using Wpf.Ui.Controls;

namespace RentToBooks.App;

public partial class MainWindow : FluentWindow
{
    private readonly AppSettingsStore _settingsStore = new();
    private AppSettings _settings;
    private IReadOnlyList<string> _lastOutputPaths = [];

    public MainWindow()
    {
        InitializeComponent();
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        ProcessTypeBox.ItemsSource = new[] { "Payment", "Invoice", "Both" };

        _settings = _settingsStore.Load();
        OutputPathBox.Text = _settings.LastOutputDirectory;
        ReceivableAccountBox.Text = _settings.ReceivableAccount;
        DepositAccountBox.Text = _settings.DepositAccount;
        IncomeAccountBox.Text = _settings.IncomeAccount;
        ProcessTypeBox.SelectedItem = _settings.ProcessType is ProcessType.Invoice or ProcessType.Both
            ? _settings.ProcessType.ToString()
            : "Payment";

        UpdateProcessAccountFields();
    }

    private ProcessType SelectedProcessType => Enum.Parse<ProcessType>((string)ProcessTypeBox.SelectedItem);

    private void ProcessTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateProcessAccountFields();

    private void UpdateProcessAccountFields()
    {
        if (ProcessTypeBox.SelectedItem is not string selected)
        {
            return;
        }

        var isInvoice = selected == "Invoice";
        var isBoth = selected == "Both";

        DepositLabel.IsEnabled = !isInvoice || isBoth;
        DepositAccountBox.IsEnabled = !isInvoice || isBoth;
        IncomeLabel.IsEnabled = isInvoice || isBoth;
        IncomeAccountBox.IsEnabled = isInvoice || isBoth;

        if (string.IsNullOrWhiteSpace(ReceivableAccountBox.Text))
        {
            ReceivableAccountBox.Text = "A11000 - Accounts Receivable";
        }

        if ((isInvoice || isBoth) && string.IsNullOrWhiteSpace(IncomeAccountBox.Text))
        {
            IncomeAccountBox.Text = "A47600 - ARB Rental Income";
        }

        if ((!isInvoice || isBoth) && string.IsNullOrWhiteSpace(DepositAccountBox.Text))
        {
            DepositAccountBox.Text = "A12000 - Undeposited Funds";
        }

        PreviewGrid.ItemsSource = null;
        OpenIifButton.IsEnabled = false;
        CopyPathButton.IsEnabled = false;
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose rent transaction report",
            Filter = "Excel workbooks (*.xlsx)|*.xlsx|All files (*.*)|*.*",
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
                ProcessTypeBox.SelectedItem = processType.ToString();
                StatusText.Text =
                    $"Detected {processType}: {detected.InvoiceRows} invoice row(s), {detected.PaymentRows} payment row(s).";
            }
            else
            {
                StatusText.Text = "Could not detect invoice or payment rows. Choose the process manually.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not auto-detect the process. Choose it manually.";
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
        var dialog = new OpenFolderDialog { Title = "Choose where to save the QuickBooks IIF file" };
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
            throw new InvalidOperationException("Choose a rent transaction report first.");
        }

        if (string.IsNullOrWhiteSpace(OutputPathBox.Text))
        {
            throw new InvalidOperationException("Choose an output folder first.");
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
            throw new InvalidOperationException("Enter the processing date in mmddyyyy format, for example 06262026.");
        }

        if (!DateTime.TryParseExact(
                text, "MMddyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new InvalidOperationException("Enter the date in mmddyyyy format, for example 06122026.");
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
        StatusText.Text = "Building preview...";
        LogBox.Text = "";

        try
        {
            var input = GetFormInput();
            var data = await Task.Run(() => RentReportConverter.BuildPreviewData(
                input.InputPath, input.ProcessingDate, input.ProcessType,
                input.ReceivableAccount, input.DepositAccount, input.IncomeAccount));

            PreviewGrid.ItemsSource = data.PreviewRows;
            StatusText.Text = "Preview ready.";
            LogBox.Text = string.Join(
                Environment.NewLine,
                $"Preview rows: {data.ProcessedRows}",
                $"Skipped rows: {data.SkippedRows}",
                "Review the rows below before creating the IIF file.");
            SaveSettings(input);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not build preview.";
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
        StatusText.Text = "Creating IIF file...";
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
                    "The following IIF file already exists and will be replaced:",
                    "",
                    string.Join(Environment.NewLine, existingOutputPaths),
                    "",
                    "Continue?");

                var choice = System.Windows.MessageBox.Show(
                    message,
                    "Replace Existing IIF File",
                    System.Windows.MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (choice != System.Windows.MessageBoxResult.Yes)
                {
                    StatusText.Text = "Create IIF canceled.";
                    LogBox.Text = "No files were changed.";
                    return;
                }
            }

            var result = await Task.Run(() => RentReportConverter.ConvertToIif(
                input.InputPath, input.OutputDirectory, input.ProcessingDate, input.ProcessType,
                input.ReceivableAccount, input.DepositAccount, input.IncomeAccount));

            _lastOutputPaths = result.OutputPaths;
            PreviewGrid.ItemsSource = result.PreviewRows;

            StatusText.Text = "IIF file created.";
            LogBox.Text = string.Join(
                Environment.NewLine,
                "Created QuickBooks IIF file.",
                $"Output: {string.Join(Environment.NewLine, result.OutputPaths)}",
                $"Processed {input.ProcessType.ToString().ToLowerInvariant()} rows: {result.ProcessedRows}",
                $"Skipped rows: {result.SkippedRows}",
                "",
                "The output is plain tab-delimited text with an .iif extension.");
            OpenFolderButton.IsEnabled = true;
            OpenIifButton.IsEnabled = _lastOutputPaths.Count == 1;
            CopyPathButton.IsEnabled = true;
            SaveSettings(input);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not create IIF file.";
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

        var psi = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        psi.ArgumentList.Add(directory);
        Process.Start(psi);
    }

    private void OpenIifButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOutputPaths.Count != 1 || !File.Exists(_lastOutputPaths[0]))
        {
            return;
        }

        var psi = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
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
        StatusText.Text = "Output path copied to clipboard.";
    }
}
