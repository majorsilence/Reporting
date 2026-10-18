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

## Theming and white-labelling

The viewer and designer are Majorsilence.Forms controls, so they follow its
[CSS themes](https://github.com/majorsilence/Majorsilence.Forms/blob/main/docs/theming.md).

- **The whole application:** call `Theme.SetBuiltInTheme (BuiltInTheme.Dark)` or `Theme.LoadFromCssFile ("theme.css")`.
  The stand-alone designer does this from **Tools > Options > Appearance**.
- **One designer or viewer:** give it its own theme without touching the rest of the host's UI.
  - `rdlUserControl.UseTheme ("Dark")` takes a built-in theme name.
  - `rdlUserControl.UseTheme (DesignerTheme.Custom, "theme.css")` or `UseThemeCss (css)` takes a CSS theme.
  - Any viewer or designer control also accepts `control.ThemeScope = scope` with a Majorsilence.Forms `ThemeScope`.

  Its dialogs follow it. `Examples/SampleDesignerControl` shows two designers in one window, each with its own theme.
- **Designer colours:** besides the Forms tokens, a theme can set the designer's own colours with `--rdl-*` variables
  (the area behind the page, bands, rulers, selection handles, ...).
  [`ReportDesigner/Themes/graphite.css`](../../ReportDesigner/Themes/graphite.css) lists every variable.
  The report page itself always stays white, so the canvas matches the output.

To ship the designer under your own name, set `ReportingBranding.Current` at startup, before creating a designer or viewer:

```csharp
ReportingBranding.Current = new ReportingBranding {
    DesignerName = "Acme Report Studio",        // window title, message boxes, About box
    ProductName = "Acme Reports",
    Icon = new Icon ("acme.ico"),
    HelpUrl = "https://acme.example/help",
    SupportUrl = "https://acme.example/support",
    WebsiteUrl = "https://acme.example",
    AboutText = "Copyright Acme Ltd.",
    SettingsFolderName = "AcmeReportStudio",    // keeps your settings apart from the stock designer's
};
```

- Anything left unset keeps the built-in text.
- `ShowAboutBox = false` hides Help > About.
- The About box always keeps the fyiReporting licence notice, which the licence requires. `AboutText` is shown above it.
