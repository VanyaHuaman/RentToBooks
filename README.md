# RentToBooks

A native desktop app that converts Avena rent transaction Excel reports into QuickBooks `.iif` import files.

This is the successor to [RentIifConverter](https://github.com/VanyaHuaman/RentIifConverter), rebuilt as a
platform-native app instead of a PowerShell/WinForms script.

## Status

- **Windows**: in progress (WPF, Fluent-styled via WPF-UI)
- **macOS**: planned, native (SwiftUI/AppKit), not started

The Windows and macOS apps share no code — each is a native implementation of the same conversion rules and
`.iif` output format.

## Repo layout

```
windows/   .NET solution: RentToBooks.Core (conversion logic), RentToBooks.Core.Tests, RentToBooks.App (WPF UI)
```

A `mac/` folder will be added when that work starts.
