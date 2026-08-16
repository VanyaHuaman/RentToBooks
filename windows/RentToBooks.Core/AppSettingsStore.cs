using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentToBooks.Core;

public class AppSettingsStore(string settingsPath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public AppSettingsStore() : this(DefaultSettingsPath())
    {
    }

    public string SettingsPath { get; } = settingsPath;

    public static string DefaultSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RentToBooks", "settings.json");

    public AppSettings Load()
    {
        var defaults = Defaults();
        if (!File.Exists(SettingsPath))
        {
            return defaults;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(SettingsPath), JsonOptions);
            if (dto is null)
            {
                return defaults;
            }

            return new AppSettings(
                string.IsNullOrWhiteSpace(dto.LastInputDirectory) ? defaults.LastInputDirectory : dto.LastInputDirectory,
                string.IsNullOrWhiteSpace(dto.LastOutputDirectory) ? defaults.LastOutputDirectory : dto.LastOutputDirectory,
                dto.ProcessType ?? defaults.ProcessType,
                string.IsNullOrWhiteSpace(dto.ReceivableAccount) ? defaults.ReceivableAccount : dto.ReceivableAccount,
                string.IsNullOrWhiteSpace(dto.DepositAccount) ? defaults.DepositAccount : dto.DepositAccount,
                string.IsNullOrWhiteSpace(dto.IncomeAccount) ? defaults.IncomeAccount : dto.IncomeAccount);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return defaults;
        }
    }

    public void Save(
        string? inputFile,
        string outputDirectory,
        ProcessType processType,
        string receivableAccount,
        string depositAccount,
        string incomeAccount)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var current = Load();
        var inputDirectory = !string.IsNullOrWhiteSpace(inputFile) && File.Exists(inputFile)
            ? Path.GetDirectoryName(inputFile) ?? current.LastInputDirectory
            : current.LastInputDirectory;

        var settings = new AppSettings(
            inputDirectory, outputDirectory, processType, receivableAccount, depositAccount, incomeAccount);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static AppSettings Defaults()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return new AppSettings(
            documents,
            documents,
            ProcessType.Payment,
            "A11000 - Accounts Receivable",
            "A12000 - Undeposited Funds",
            "A47600 - ARB Rental Income");
    }

    private sealed record StoredSettings(
        string? LastInputDirectory,
        string? LastOutputDirectory,
        ProcessType? ProcessType,
        string? ReceivableAccount,
        string? DepositAccount,
        string? IncomeAccount);
}
