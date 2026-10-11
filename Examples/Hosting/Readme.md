# Hosting the report viewer in each UI framework

The report viewer (`Majorsilence.Reporting.RdlViewer.RdlViewer`) is a Majorsilence.Forms control. Each host
package turns it into a native element for that framework. See the wiki page
[27x Hosting Controls](https://github.com/majorsilence/Reporting/wiki/27x-Hosting-Controls).

| Folder | Framework | Host package | Embed call |
|---|---|---|---|
| `WinForms` | Windows Forms | `Majorsilence.Forms.WinForms` | `ToWinFormsControl()` |
| `Wpf` | WPF | `Majorsilence.Forms.Wpf` | `ToWpfElement()` |
| `Avalonia` | Avalonia | `Majorsilence.Forms.Avalonia` | `ToAvaloniaControl()` |
| `Uno` | Uno Platform (Skia desktop) | `Majorsilence.Forms.Uno` | `ToUnoControl()` |
| `Gtk4` | GTK 4 (gir.core) | `Majorsilence.Forms.Gtk4` | `ToGtkWidget()` |
| `Terminal` | Terminal | `Majorsilence.Forms.Terminal` | `TerminalApplication.Use()` |

`Shared/ReportContent.cs` builds the viewer and toolstrip once; each host only embeds it. Keep a reference to
the viewer and call its normal API (`SetSourceFile`, `Rebuild`, `SaveAs`); the native element is only a container.

```
dotnet run --project Examples/Hosting/Avalonia [report.rdl]
```

WinForms and WPF run on Windows only. The default report is `Products.rdl` with its SQLite database.
