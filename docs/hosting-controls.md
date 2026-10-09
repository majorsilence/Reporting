# Hosting the viewer and designer in your UI framework

The report viewer (`Majorsilence.Reporting.RdlViewer`) and designer (`Majorsilence.Reporting.RdlDesign`)
are [Majorsilence.Forms](https://github.com/majorsilence/Majorsilence.Forms) controls. Majorsilence.Forms
draws them itself with SkiaSharp and ships a **host package per UI framework**. Each host turns the
control into a native element of that framework with a `To...()` extension method, so there is no
Reporting-specific host code to maintain.

| Your app uses | Install | Embed with |
| --- | --- | --- |
| Majorsilence.Forms (cross-platform, WinForms-style API) | `Majorsilence.Forms` + a backend, usually `Majorsilence.Forms.Avalonia` | use the control directly, `Application.Run(form)` |
| **System.Windows.Forms** | `Majorsilence.Forms.WinForms` | `control.ToWinFormsControl()` |
| **WPF** | `Majorsilence.Forms.Wpf` | `control.ToWpfElement()` |
| **Avalonia** | `Majorsilence.Forms.Avalonia` | `control.ToAvaloniaControl()` |
| **Uno Platform** | `Majorsilence.Forms.Uno` | `control.ToUnoControl()` |
| **GTK 4** | `Majorsilence.Forms.Gtk4` | `control.ToGtkWidget()` |
| **Terminal** | `Majorsilence.Forms.Terminal` | the viewer *is* the app (`TerminalApplication.Use()`); nothing to embed into |

`Form`s have a matching window method: `ToWinFormsForm()`, `ToWpfWindow()`, `ToAvaloniaWindow()`,
`ToUnoWindow()`, `ToGtkWindow()`.

Keep a reference to the `RdlViewer` (or `RdlUserControl`) instance and call its normal API on it
(`SetSourceFile`, `Rebuild`, `SaveAs`, ...). The element returned by `To...()` is only the host.

Every host's package page and `samples/Embedding*` folder in the Majorsilence.Forms repository has a
complete runnable project; see also its
[platform backends](https://github.com/majorsilence/Majorsilence.Forms/blob/main/docs/backends.md) page.

> Only the cross-platform pieces are exercised by this repository's CI (Linux, macOS and Windows
> build and test). The WinForms, WPF and Uno snippets below depend on a Windows (or desktop) UI stack
> and have not been run by our CI; the method names come from the Majorsilence.Forms packages.
> Check them against the Majorsilence.Forms version you install.

In every snippet below:

```csharp
var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { Dock = Majorsilence.Forms.DockStyle.Fill };
```

## System.Windows.Forms (existing WinForms apps)

This is how existing WinForms users keep embedding the same controls they had before the move to
Majorsilence.Forms: add the WinForms host and wrap the control.

```bash
dotnet add package Majorsilence.Reporting.RdlViewer
dotnet add package Majorsilence.Forms.WinForms
```

```csharp
using Majorsilence.Forms.WinForms;

System.Windows.Forms.Control host = viewer.ToWinFormsControl();
host.Dock = System.Windows.Forms.DockStyle.Fill;
myWinFormsForm.Controls.Add(host);

await viewer.SetSourceFile(new Uri(rdlPath));
```

Use `Majorsilence.Forms.WinForms.MajorsilenceFormsPresenter` directly (`new MajorsilenceFormsPresenter { Content = viewer }`)
if you prefer a named control in the Visual Studio toolbox. Popups (combo dropdowns, menus, tooltips)
are real borderless windows, so they are not clipped by the host form.

The designer works the same way: `new Majorsilence.Reporting.RdlDesign.RdlUserControl().ToWinFormsControl()`.

## WPF

`LibRdlWpfViewer` (a WPF `UserControl` wrapping the viewer in a `WindowsFormsHost`) has been retired;
use the regular viewer with the WPF host instead. The project must target `net8.0-windows` or
`net10.0-windows` and set `<UseWPF>true</UseWPF>`.

```bash
dotnet add package Majorsilence.Reporting.RdlViewer
dotnet add package Majorsilence.Forms.Wpf
```

```xml
<!-- MainWindow.xaml -->
<Window x:Class="MyApp.MainWindow" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Title="Reports">
    <Grid x:Name="ViewerHost" />
</Window>
```

```csharp
using Majorsilence.Forms.Wpf;

public partial class MainWindow : System.Windows.Window
{
    private readonly Majorsilence.Reporting.RdlViewer.RdlViewer _viewer = new() { Dock = Majorsilence.Forms.DockStyle.Fill };

    public MainWindow()
    {
        InitializeComponent();
        ViewerHost.Children.Add(_viewer.ToWpfElement());   // a MajorsilenceFormsPresenter (WPF Grid)
    }

    public Task ShowReport(Uri rdl) => _viewer.SetSourceFile(rdl);
}
```

If no Majorsilence.Forms backend is configured, `ToWpfElement()` installs the WPF one for you.

| LibRdlWpfViewer | Now |
| --- | --- |
| `new RdlWpfViewer()` | `new RdlViewer().ToWpfElement()` |
| `viewer.SetSourceFile(uri)` | `rdlViewer.SetSourceFile(uri)` |
| `viewer.Rebuild()` | `rdlViewer.Rebuild()` |
| `viewer.SaveAs(...)` | `rdlViewer.SaveAs(...)` |

## Avalonia

```bash
dotnet add package Majorsilence.Reporting.RdlViewer
dotnet add package Majorsilence.Forms.Avalonia
```

```csharp
using Majorsilence.Forms.Avalonia;

Avalonia.Controls.Control host = viewer.ToAvaloniaControl();
myAvaloniaPanel.Children.Add(host);
```

## Uno Platform

```bash
dotnet add package Majorsilence.Reporting.RdlViewer
dotnet add package Majorsilence.Forms.Uno
```

```csharp
using Majorsilence.Forms.Uno;

Microsoft.UI.Xaml.FrameworkElement host = viewer.ToUnoControl();
myUnoGrid.Children.Add(host);
```

## GTK 4

```bash
dotnet add package Majorsilence.Reporting.RdlViewer
dotnet add package Majorsilence.Forms.Gtk4
```

```csharp
using Majorsilence.Forms.Gtk4;

Gtk.Widget widget = viewer.ToGtkWidget();
someGtkBox.Append(widget);
```

`RdlGtk3Viewer` in this repository is the older GTK 3 viewer and is separate from this route.

## Terminal

There is no host application to embed into: the terminal backend *is* the app. The viewer runs in a
terminal using Kitty graphics, Sixel or block characters, with mouse and keyboard input. Ctrl+C exits.

```bash
dotnet add package Majorsilence.Reporting.RdlViewer
dotnet add package Majorsilence.Forms.Terminal
```

```csharp
using Majorsilence.Forms;
using Majorsilence.Forms.Terminal;

TerminalApplication.Use();
var form = new Form();
var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { Dock = DockStyle.Fill };
form.Controls.Add(viewer);
form.Load += async (_, _) => await viewer.SetSourceFile(new Uri(rdlPath));
Application.Run(form);
```

## Majorsilence.Forms directly (no host framework)

Install `Majorsilence.Forms` and a backend (`Majorsilence.Forms.Avalonia` is the cross-platform
default), put the viewer on a `Form`, and call `Application.Run(form)`. `RdlReader` and
`ReportDesigner` in this repository are built this way.
