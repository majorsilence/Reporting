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

## Embedding the designer in WPF (issue #470)

There are two supported ways to put `RdlUserControl` (the embeddable designer) in a WPF window, depending on
which package you're on:

- **27.x — `ToWpfElement()`.** `RdlUserControl` is a Majorsilence.Forms control like the viewer, so it follows
  the same pattern as `Examples/Hosting/Wpf` above: `designer.ToWpfElement()` returns a
  `System.Windows.FrameworkElement` directly, with no `WindowsFormsHost` involved.
- **26.0.x (legacy) — `WindowsFormsHost`.** Before 27.x, embed `RdlUserControl` the same way as any WinForms
  control in WPF:

  ```csharp
  var designer = new Majorsilence.Reporting.RdlDesign.RdlUserControl();
  var host = new System.Windows.Forms.Integration.WindowsFormsHost { Child = designer };
  myWpfWindow.Content = host;
  ```

  Under `WindowsFormsHost` there is no WPF-native owner for a WinForms dialog box, and the control itself
  may have no WinForms `Form` ancestor at all — so any dialog the designer opens must be given the control
  itself as its owner (`MessageBox.Show(this, ...)`, not `MessageBox.Show(this.FindForm(), ...)` or no owner
  at all), since the control's own window handle is the only thing guaranteed to exist in that host. A dialog
  opened with no owner, or owned by a `Form` that doesn't exist, can appear behind the WPF window — which
  looks like a hang rather than a dialog waiting for input. That was exactly the cause of a reported
  `RdlUserControl.GetRdlText()` "hang": `DesignCtl`'s report-source getter caught an exception and showed an
  owner-less "Unable to create RDL" message box, which opened behind the host window. This has been audited
  and fixed for every `MessageBox.Show` call reachable from `RdlUserControl`'s own code path
  (`RdlUserControl.cs`, `RdlEditPreview.cs`, `DesignCtl.cs`) — each now passes `this` as the owner.

`RdlUserControl`'s document lifecycle (`OpenFile`/`SourceRdl`, `Modified`, `RdlChanged`, `GetRdlText()`) is the
same under either embedding path; see `EmbeddedDesignerTests` for the behavior this is tested against.

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
