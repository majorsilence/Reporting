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

  Its dialogs follow it. `Examples/SampleDesignerControl` shows two designers in one window, each with its own
  theme, with a "Toggle theme" button above each one so you can flip between Light and Dark without restarting
  (issue #471). Surfaces you can compare across both themes in that one running sample:
  - Toolbar, menus, PropertyGrid, canvas, bands and selection handles — visible as soon as the report loads;
    select the Table or a cell to see selection handles and the PropertyGrid follow the theme.
  - The Edit tab's SQL/XML syntax highlighting — click the designer's **Edit** tab.
  - The expression editor dialog — select a cell with a `=Fields!...` value and open its `Value` property's
    "..." button in the PropertyGrid.

  The Data Sources and Dataset dialogs (below) follow the theme too, once a host wires up a button to open them —
  `RdlUserControl` has no built-in toolbar/menu entry for either, unlike the stand-alone shell.
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

## Host-supplied toolbar/menu icons

`RdlUserControl.IconProvider` (and `RdlDesigner.IconProvider` for the stand-alone shell) lets a host override
individual toolbar/menu icons, keyed by a stable name (the icon's resource name in
`RdlDesign/Properties/Resources.resx`, e.g. `"document_new"`, `"edit_cut"`, `"format_justify_left"`):

```csharp
class DarkIcons : IDesignerIconProvider
{
    public Image GetIcon(string name) => name switch
    {
        "document_new" => MyDarkIcons.New,
        "edit_cut" => MyDarkIcons.Cut,
        _ => null,          // keep the built-in icon for everything else
    };
}

designer.IconProvider = new DarkIcons();
```

Returning `null` for a name keeps the built-in raster icon, so a host can override as few or as many icons as
it wants. No provider (the default) is identical to before this property existed.

## Data Sources and Dataset dialogs on the embeddable designer (issue #469/#470)

Unlike the stand-alone `RdlDesigner` MDI shell, `RdlUserControl` has no built-in toolbar button or menu for
managing data sources or datasets — a host wires its own UI to call these:

```csharp
// Host-provided "Data Sources..." button:
designer.ShowDataSourcesDialog();

// Host-provided "Datasets" menu, built from the report's current datasets:
foreach (string name in designer.GetDataSetNames())
    AddMenuItem(name, () => designer.ShowDataSetDialog(name));
AddMenuItem("New...", () => designer.ShowDataSetDialog());   // no name -- creates a new dataset
```

Both raise a cancellable event first — `DataSourcesPropertiesRequested` / `DataSetPropertiesRequested` — so a
host can show its own UI instead of the native dialog, the same pattern as "Overriding toolbar/menu actions"
above but on `RdlUserControl` rather than `RdlDesigner`:

```csharp
designer.DataSetPropertiesRequested += (s, e) =>
{
    if (e.DataSetName == "Orders")
    {
        MyOwnDatasetEditor.Show(e.DataSetName);
        e.Handled = true;      // skip the native dataset-properties dialog
    }
    // otherwise fall through to the native dialog for every other dataset
};
```

No subscribers (the default) runs the native dialog exactly as calling these methods always has.
