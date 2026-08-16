# RentToBooks

A native desktop app that converts Avena rent transaction Excel reports into QuickBooks `.iif` import files.

Current version: `0.1.0`

This is the successor to [RentIifConverter](https://github.com/VanyaHuaman/RentIifConverter), rebuilt as a
platform-native app instead of a PowerShell/WinForms script.

## Status

- **Windows**: available (WPF, Fluent-styled via WPF-UI)
- **macOS**: planned, native (SwiftUI/AppKit), not started

The Windows and macOS apps share no code — each is a native implementation of the same conversion rules and
`.iif` output format.

## Requirements

- Windows
- A rent transaction report named like `RentTransactionDetailReport 06122026.xlsx`

RentToBooks reads `.xlsx` files directly and does not require Excel, PowerShell, or the .NET runtime to be
installed — the published app is a single self-contained `.exe`.

## Get The Tool

Download the latest `RentToBooks.App.exe` from the
[Releases page](https://github.com/VanyaHuaman/RentToBooks/releases) and run it. No installer, no setup.

## Use

1. Launch `RentToBooks.App.exe`.
2. Browse to the rent transaction report `.xlsx`.
3. Confirm the processing date. RentToBooks fills this from the filename when it finds a valid `mmddyyyy`
   date; otherwise you must enter it.
4. Confirm the output folder (independent of the report's folder — it remembers its own last-used location).
5. Choose the process type:
   - `Payment` uses column I and creates `PAYMENT` rows.
   - `Invoice` uses column G and creates `INVOICE` rows.
   - `Both` creates separate payment and invoice `.iif` files from one mixed report.
   - RentToBooks tries to detect this automatically when you choose a report.
6. Confirm the QuickBooks account names.
7. Click `Preview` to review the rows before writing anything.
8. Click `Create IIF`.

The generated file is tab-delimited text, not an Excel workbook renamed with `.iif`.

## Notes

- The output folder defaults to your last-used output folder, independent of the input file's folder.
- RentToBooks remembers the last folders and account names (`%AppData%\RentToBooks\settings.json`).
- Payment output uses `A12000 - Undeposited Funds` for `TRNS` and `A11000 - Accounts Receivable` for `SPL`.
- Invoice output uses `A11000 - Accounts Receivable` for `TRNS` and `A47600 - ARB Rental Income` for `SPL`.
- `Open IIF` opens the generated file in Notepad. `Copy Path` copies the generated file path to the clipboard.
- RentToBooks validates that the selected workbook has the expected `Tenant`, `Datetime`, and `Payment`
  columns before processing.
- You'll be asked to confirm before an existing `.iif` file with the same name is replaced.

## Development

```
windows/
  RentToBooks.Core/         Conversion logic (XLSX reading, IIF generation, settings)
  RentToBooks.Core.Tests/   xUnit regression tests, byte-compare output against known-good fixtures
  RentToBooks.App/          WPF UI
```

All user-facing text lives in `.resx` resource files (`RentToBooks.Core/Resources/CoreMessages.resx`,
`RentToBooks.App/Resources/AppStrings.resx`) — adding another language later is a matter of adding a
culture-specific `.resx` file alongside the English one, no code changes required.

Run tests:

```powershell
cd windows
dotnet test
```

Run the app from source:

```powershell
cd windows
dotnet run --project RentToBooks.App
```

Publish a self-contained single-file build:

```powershell
cd windows/RentToBooks.App
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

A `mac/` folder will be added when the macOS app work starts.
