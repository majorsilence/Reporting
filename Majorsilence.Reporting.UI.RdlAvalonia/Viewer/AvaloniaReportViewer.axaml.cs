using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlEngine;

namespace Majorsilence.Reporting.UI.RdlAvalonia.Viewer
{
    public partial class AvaloniaReportViewer : UserControl
    {
        private Report? _report;
        private Pages? _pages;
        private Uri? _sourceFile;
        private string? _sourceRdl;
        private IDictionary _parameters = new Dictionary<string, string>();
        private IList? _errorMessages;
        private int _pageCurrent = 1;
        private double _zoom = 1.0;
        private ZoomMode _zoomMode = ZoomMode.FitWidth;

        // Panning
        private bool _isPanning;
        private Point _panStart;

        // Search
        private record SearchMatch(int PageIndex, PageItem Item);
        private readonly List<SearchMatch> _searchResults = new();
        private int _searchIndex = -1;
        private string _lastSearchText = string.Empty;

        // Thumbnails
        private readonly List<Border> _thumbnailBorders = new();
        private bool _thumbnailsDirty = true;
        private CancellationTokenSource? _thumbnailCts;

        private sealed class ZoomOption
        {
            public string Label { get; init; } = string.Empty;
            public ZoomMode? Mode { get; init; }
            public double? Fixed { get; init; }
            public override string ToString() => Label;
        }

        public AvaloniaReportViewer()
        {
            InitializeComponent();
            RdlEngineConfig.GetCustomReportTypes();
            InitializeUi();
        }

        public event EventHandler<SubreportDataRetrievalEventArgs>? SubreportDataRetrieval;

        public string? ConnectionStringOverride { get; private set; }

        public bool OverwriteSubreportConnection { get; private set; }

        public string? WorkingDirectory { get; set; }

        public async Task SetSourceFileAsync(Uri fileUri)
        {
            _sourceFile = fileUri;
            _sourceRdl = null;
            WorkingDirectory = Path.GetDirectoryName(fileUri.LocalPath);
            await RebuildAsync();
        }

        public async Task SetSourceRdlAsync(string rdl)
        {
            _sourceRdl = rdl;
            _sourceFile = null;
            await RebuildAsync();
        }

        public void SetReportParametersAmpersandSeparated(string parameterString)
        {
            _parameters = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(parameterString))
            {
                return;
            }

            string[] prms = parameterString.TrimEnd(';').Split('&');
            foreach (string p in prms)
            {
                int iEq = p.IndexOf("=", StringComparison.Ordinal);
                if (iEq > 0)
                {
                    string name = p.Substring(0, iEq);
                    string val = p.Substring(iEq + 1);
                    _parameters.Add(name, val);
                }
            }
        }

        public async Task RebuildAsync()
        {
            if (_sourceFile == null && string.IsNullOrWhiteSpace(_sourceRdl))
            {
                return;
            }

            LoadingOverlay.IsVisible = true;
            EmptyStatePanel.IsVisible = false;
            PageBorder.IsVisible = false;

            try
            {
                _report = await GetReportAsync();
                if (_report == null)
                {
                    return;
                }

                _pages = await BuildPagesAsync(_report);
                _pageCurrent = 1;

                ReportCanvas.SetReport(_report, _pages);
                UpdatePageUi();
                UpdateErrorsUi();
                BuildParameterUi();
                _thumbnailsDirty = true;
                if (ThumbnailPanel.IsVisible)
                    await BuildThumbnailsAsync();
            }
            catch (Exception ex)
            {
                _report = null;
                _pages = null;
                _sourceFile = null;
                _sourceRdl = null;
                ReportCanvas.SetReport(null, null);
                UpdatePageUi();
                UpdateErrorsUi();
                await ShowErrorAsync($"Failed to load report:\n\n{ex.Message}");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var owner = topLevel as Window;

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 80
            };

            var dialog = new Window
            {
                Title = "Report Error",
                Width = 480,
                MinHeight = 160,
                MaxHeight = 420,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = TextWrapping.Wrap
                        },
                        okButton
                    }
                }
            };

            okButton.Click += (_, _) => dialog.Close();

            if (owner != null)
                await dialog.ShowDialog(owner);
            else
                dialog.Show();
        }

        private void InitializeUi()
        {
            var zoomOptions = new ZoomOption[]
            {
                new() { Label = "Fit Width",   Mode = ZoomMode.FitWidth },
                new() { Label = "Fit Page",    Mode = ZoomMode.FitPage },
                new() { Label = "Actual Size", Mode = ZoomMode.ActualSize },
                new() { Label = "─────",       Fixed = -1 },    // visual separator (disabled by value)
                new() { Label = "50 %",  Fixed = 0.50 },
                new() { Label = "75 %",  Fixed = 0.75 },
                new() { Label = "100 %", Fixed = 1.00 },
                new() { Label = "125 %", Fixed = 1.25 },
                new() { Label = "150 %", Fixed = 1.50 },
                new() { Label = "200 %", Fixed = 2.00 },
            };
            ZoomModeComboBox.ItemsSource = zoomOptions;
            ZoomModeComboBox.SelectedIndex = 0;
            UpdateStatusZoom();

            OpenButton.Click += OpenButtonOnClick;
            SaveButton.Click += SaveButtonOnClick;
            PrintButton.Click += PrintButtonOnClick;
            ReloadButton.Click += async (_, _) => await RebuildAsync();
            CopyButton.Click += (_, _) => ReportCanvas.CopySelection();
            SelectAllButton.Click += (_, _) => ReportCanvas.SelectAll();
            CopyImageButton.Click += async (_, _) => await ReportCanvas.SavePageAsPngAsync();
            FirstPageButton.Click += (_, _) => SetPage(1);
            PreviousPageButton.Click += (_, _) => SetPage(_pageCurrent - 1);
            NextPageButton.Click += (_, _) => SetPage(_pageCurrent + 1);
            LastPageButton.Click += (_, _) => SetPage(_pages?.PageCount ?? 1);
            ZoomInButton.Click  += (_, _) => SetZoom(_zoom + 0.25);
            ZoomOutButton.Click += (_, _) => SetZoom(Math.Max(0.25, _zoom - 0.25));
            ZoomModeComboBox.SelectionChanged += ZoomModeComboBoxOnSelectionChanged;
            PageTextBox.LostFocus += PageTextBoxOnLostFocus;
            PageTextBox.KeyDown += PageTextBoxOnKeyDown;
            ApplyParametersButton.Click += ApplyParametersButtonOnClick;
            ErrorsToggleButton.IsCheckedChanged += ErrorsToggleOnChanged;
            ThumbnailsButton.IsCheckedChanged += ThumbnailsButtonOnCheckedChanged;

            // Find bar
            FindButton.Click += (_, _) => OpenFindBar();
            FindTextBox.KeyDown += FindTextBoxOnKeyDown;
            FindTextBox.TextChanged += (_, _) => ExecuteSearch(FindTextBox.Text ?? string.Empty);
            FindNextButton.Click += (_, _) => FindNavigate(+1);
            FindPrevButton.Click += (_, _) => FindNavigate(-1);
            FindCloseButton.Click += (_, _) => CloseFindBar();

            // Panning (middle-mouse drag)
            ReportScrollViewer.AddHandler(PointerPressedEvent, OnScrollViewerPointerPressed, handledEventsToo: false);
            ReportScrollViewer.AddHandler(PointerMovedEvent, OnScrollViewerPointerMoved, handledEventsToo: true);
            ReportScrollViewer.AddHandler(PointerReleasedEvent, OnScrollViewerPointerReleased, handledEventsToo: true);
            ReportScrollViewer.AddHandler(PointerCaptureLostEvent, OnScrollViewerPointerCaptureLost, handledEventsToo: true);

            ReportScrollViewer.SizeChanged += (_, _) => ApplyZoomMode();
            ReportScrollViewer.AddHandler(PointerWheelChangedEvent, OnScrollViewerPointerWheelChanged, handledEventsToo: false);
            AddHandler(KeyDownEvent, OnViewerKeyDown, handledEventsToo: false);
        }

        private async void OnViewerKeyDown(object? sender, KeyEventArgs e)
        {
            var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            if (e.Key == Key.F5)
            {
                await RebuildAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                SetPage(_pageCurrent + 1);
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                SetPage(_pageCurrent - 1);
                e.Handled = true;
            }
            else if (e.Key == Key.Home && !ctrl)
            {
                SetPage(1);
                e.Handled = true;
            }
            else if (e.Key == Key.End && !ctrl)
            {
                SetPage(_pages?.PageCount ?? 1);
                e.Handled = true;
            }
            else if (ctrl && (e.Key == Key.OemPlus || e.Key == Key.Add))
            {
                SetZoom(_zoom + 0.25);
                e.Handled = true;
            }
            else if (ctrl && (e.Key == Key.OemMinus || e.Key == Key.Subtract))
            {
                SetZoom(Math.Max(0.25, _zoom - 0.25));
                e.Handled = true;
            }
            else if (ctrl && (e.Key == Key.D0 || e.Key == Key.NumPad0))
            {
                SetZoom(1.0);
                e.Handled = true;
            }
            else if (ctrl && e.Key == Key.F)
            {
                OpenFindBar();
                e.Handled = true;
            }
        }

        private void PageTextBoxOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(PageTextBox.Text, out var page))
                    SetPage(page);
                e.Handled = true;
            }
        }

        private void OnScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                // Ctrl+Wheel = zoom in/out
                var delta = e.Delta.Y > 0 ? 0.1 : -0.1;
                SetZoom(Math.Max(0.1, _zoom + delta));
                e.Handled = true;
            }
        }

        private async void OpenButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Open RDL Report",
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("RDL Files") { Patterns = new[] { "*.rdl" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count == 0)
            {
                return;
            }

            await SetSourceFileAsync(files[0].Path);
        }

        private async void SaveButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (_report == null)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Report",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("HTML") { Patterns = new[] { "*.html", "*.htm" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("XML") { Patterns = new[] { "*.xml" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("MHTML") { Patterns = new[] { "*.mhtml", "*.mht" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("RTF") { Patterns = new[] { "*.rtf" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("TIFF") { Patterns = new[] { "*.tif", "*.tiff" } }
                }
            });

            if (file == null)
            {
                return;
            }

            var filePath = file.Path.LocalPath;
            var outputType = OutputPresentationType.Internal;
            var ext = Path.GetExtension(filePath).Trim('.').ToLowerInvariant();
            switch (ext)
            {
                case "pdf":
                    outputType = OutputPresentationType.PDF;
                    break;
                case "xml":
                    outputType = OutputPresentationType.XML;
                    break;
                case "html":
                case "htm":
                    outputType = OutputPresentationType.HTML;
                    break;
                case "csv":
                    outputType = OutputPresentationType.CSV;
                    break;
                case "mht":
                case "mhtml":
                    outputType = OutputPresentationType.MHTML;
                    break;
                case "rtf":
                    outputType = OutputPresentationType.RTF;
                    break;
                case "xlsx":
                    outputType = OutputPresentationType.Excel2007;
                    break;
                case "tif":
                case "tiff":
                    outputType = OutputPresentationType.TIF;
                    break;
            }

            await SaveAsAsync(filePath, outputType);
        }

        private async void PrintButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (_report == null)
            {
                return;
            }

            // Avalonia does not expose a cross-platform print API yet; export to PDF for now.
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save as PDF for Printing",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (file == null)
            {
                return;
            }

            var filePath = file.Path.LocalPath;
            await SaveAsAsync(filePath, OutputPresentationType.PDF);
        }

        private async Task SaveAsAsync(string filePath, OutputPresentationType outputType)
        {
            if (_report == null || _pages == null)
                return;

            SetExportingUi(true);
            OneFileStreamGen? sg = null;
            try
            {
                await _report.RunGetData(_parameters);

                sg = new OneFileStreamGen(filePath, true);
                switch (outputType)
                {
                    case OutputPresentationType.PDF:
                        await _report.RunRender(sg, OutputPresentationType.PDF);
                        break;
                    case OutputPresentationType.CSV:
                        await _report.RunRender(sg, OutputPresentationType.CSV);
                        break;
                    case OutputPresentationType.RTF:
                        await _report.RunRender(sg, OutputPresentationType.RTF);
                        break;
                    case OutputPresentationType.Excel2007:
                        await _report.RunRender(sg, OutputPresentationType.Excel2007);
                        break;
                    case OutputPresentationType.XML:
                        await _report.RunRender(sg, OutputPresentationType.XML);
                        break;
                    case OutputPresentationType.HTML:
                        await _report.RunRender(sg, OutputPresentationType.HTML);
                        break;
                    case OutputPresentationType.MHTML:
                        await _report.RunRender(sg, OutputPresentationType.MHTML);
                        break;
                    case OutputPresentationType.TIF:
                        await _report.RunRender(sg, OutputPresentationType.TIF);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported export format: " + Path.GetExtension(filePath));
                }
            }
            finally
            {
                sg?.CloseMainStream();
                SetExportingUi(false);
            }
        }

        private void SetExportingUi(bool exporting)
        {
            LoadingOverlay.IsVisible = exporting;
            SaveButton.IsEnabled = !exporting;
            PrintButton.IsEnabled = !exporting;
            OpenButton.IsEnabled = !exporting;
            StatusMessageTextBlock.Text = exporting ? "Exporting…" : string.Empty;
        }

        private async Task<Report?> GetReportAsync()
        {
            string source;
            if (!string.IsNullOrWhiteSpace(_sourceRdl))
            {
                source = _sourceRdl;
            }
            else if (_sourceFile != null)
            {
#if NET6_0_OR_GREATER
                source = await File.ReadAllTextAsync(_sourceFile.LocalPath);
#else
                source = File.ReadAllText(_sourceFile.LocalPath);
#endif
            }
            else
            {
                return null;
            }

            var parser = new RDLParser(source)
            {
                Folder = WorkingDirectory,
                OverwriteConnectionString = ConnectionStringOverride,
                OverwriteInSubreport = OverwriteSubreportConnection
            };

            var report = await parser.Parse();
            report.SubreportDataRetrieval += (_, args) => SubreportDataRetrieval?.Invoke(this, args);
            return report;
        }

        private async Task<Pages?> BuildPagesAsync(Report report)
        {
            try
            {
                await report.RunGetData(_parameters);
                var pages = await report.BuildPages();
                if (report.ErrorMaxSeverity > 0)
                {
                    _errorMessages = report.ErrorItems;
                    report.ErrorReset();
                }

                return pages;
            }
            catch
            {
                return null;
            }
        }

        private void UpdatePageUi()
        {
            var hasReport = _pages != null && _pages.PageCount > 0;
            PageBorder.IsVisible = hasReport;
            EmptyStatePanel.IsVisible = !hasReport;

            if (_pages == null)
            {
                PageTextBox.Text = "0";
                PageCountTextBlock.Text = "/ 0";
                StatusPageTextBlock.Text = string.Empty;
                return;
            }

            PageTextBox.Text = _pageCurrent.ToString();
            PageCountTextBlock.Text = $"/ {_pages.PageCount}";
            StatusPageTextBlock.Text = $"Page {_pageCurrent} of {_pages.PageCount}";
            SetPage(_pageCurrent);
        }

        private void UpdateStatusZoom()
        {
            StatusZoomTextBlock.Text = $"{(int)Math.Round(_zoom * 100)} %";
        }

        private void UpdateErrorsUi()
        {
            ErrorsListBox.Items.Clear();

            var hasErrors = _errorMessages != null && _errorMessages.Count > 0;
            ErrorsToggleGroup.IsVisible = hasErrors;

            if (!hasErrors)
            {
                ErrorsPanel.IsVisible = false;
                ErrorsToggleButton.IsChecked = false;
                return;
            }

            ErrorsButtonText.Text = $"Errors ({_errorMessages!.Count})";
            ErrorsIconText.Foreground = new SolidColorBrush(Color.FromRgb(210, 105, 30));

            foreach (var message in _errorMessages)
            {
                ErrorsListBox.Items.Add(message);
            }
        }

        private void SetPage(int page)
        {
            if (_pages == null || _pages.PageCount == 0)
            {
                return;
            }

#if NET6_0_OR_GREATER
            var newPage = Math.Clamp(page, 1, _pages.PageCount);
#else
            var newPage = Math.Max(1, Math.Min(page, _pages.PageCount));
#endif
            _pageCurrent = newPage;
            PageTextBox.Text = newPage.ToString();
            ReportCanvas.SetPage(newPage - 1);
            UpdateThumbnailHighlight();
        }

        private void SetZoom(double zoom)
        {
            _zoom = zoom;
            _zoomMode = ZoomMode.ActualSize;
            ReportCanvas.SetZoom(_zoom);
            UpdateStatusZoom();
        }

        private void ApplyZoomMode()
        {
            if (_pages == null)
                return;

            var viewportWidth  = ReportScrollViewer.Viewport.Width;
            var viewportHeight = ReportScrollViewer.Viewport.Height;
            if (viewportWidth <= 1 || viewportHeight <= 1)
                return;

            var pageWidth  = _pages.PageWidth;
            var pageHeight = _pages.PageHeight;
            if (pageWidth <= 0 || pageHeight <= 0)
                return;

            const double ptsToLogical = 96.0 / 72.0;
            switch (_zoomMode)
            {
                case ZoomMode.FitPage:
                    _zoom = Math.Min(viewportWidth  / (pageWidth  * ptsToLogical),
                                     viewportHeight / (pageHeight * ptsToLogical));
                    break;
                case ZoomMode.FitWidth:
                    _zoom = viewportWidth / (pageWidth * ptsToLogical);
                    break;
                default:
                    return;
            }

            ReportCanvas.SetZoom(_zoom);
            UpdateStatusZoom();
        }

        private void ZoomModeComboBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ZoomModeComboBox.SelectedItem is not ZoomOption opt) return;

            if (opt.Mode.HasValue)
            {
                _zoomMode = opt.Mode.Value;
                if (_zoomMode == ZoomMode.ActualSize)
                    SetZoom(1.0);
                else
                    ApplyZoomMode();
            }
            else if (opt.Fixed is > 0)
            {
                SetZoom(opt.Fixed.Value);
            }
        }

        private void PageTextBoxOnLostFocus(object? sender, RoutedEventArgs e)
        {
            if (int.TryParse(PageTextBox.Text, out var page))
            {
                SetPage(page);
            }
        }

        private async void ApplyParametersButtonOnClick(object? sender, RoutedEventArgs e)
        {
            CollectParametersFromUi();
            await RebuildAsync();
        }

        private void ErrorsToggleOnChanged(object? sender, RoutedEventArgs e)
        {
            ErrorsPanel.IsVisible = ErrorsToggleButton.IsChecked == true;
        }

        // ── Thumbnails ───────────────────────────────────────────────

        private async void ThumbnailsButtonOnCheckedChanged(object? sender, RoutedEventArgs e)
        {
            var show = ThumbnailsButton.IsChecked == true;
            ThumbnailPanel.IsVisible = show;
            if (show && _thumbnailsDirty && _pages != null)
                await BuildThumbnailsAsync();
        }

        private async Task BuildThumbnailsAsync()
        {
            _thumbnailCts?.Cancel();
            _thumbnailCts = new CancellationTokenSource();
            var ct = _thumbnailCts.Token;

            ThumbnailStack.Children.Clear();
            _thumbnailBorders.Clear();

            if (_pages == null) return;

            const double thumbWidth = 120.0;

            for (int i = 0; i < _pages.PageCount; i++)
            {
                if (ct.IsCancellationRequested) return;

                var pageNum = i + 1;
                var isCurrentPage = pageNum == _pageCurrent;

                var imgControl = new Avalonia.Controls.Image
                {
                    Width = thumbWidth,
                    Stretch = Avalonia.Media.Stretch.Fill
                };

                var imgBorder = new Border
                {
                    BorderThickness = new Thickness(2),
                    BorderBrush = isCurrentPage
                        ? new SolidColorBrush(Color.FromRgb(51, 102, 204))
                        : new SolidColorBrush(Colors.Transparent),
                    BoxShadow = BoxShadows.Parse("0 1 4 0 #28000000"),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = imgControl
                };
                var capturedNum = pageNum;
                imgBorder.PointerPressed += (_, _) => SetPage(capturedNum);
                _thumbnailBorders.Add(imgBorder);

                var label = new TextBlock
                {
                    Text = $"Page {pageNum}",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0.6
                };

                var container = new StackPanel { Spacing = 3 };
                container.Children.Add(imgBorder);
                container.Children.Add(label);
                ThumbnailStack.Children.Add(container);

                var pages = _pages;
                var pageIdx = i;
                var bitmap = await Task.Run(() => ReportCanvas.RenderPageThumbnail(pages, pageIdx, thumbWidth), ct);
                if (ct.IsCancellationRequested) return;
                imgControl.Source = bitmap;
            }

            _thumbnailsDirty = false;
        }

        private void UpdateThumbnailHighlight()
        {
            var highlight = new SolidColorBrush(Color.FromRgb(51, 102, 204));
            var transparent = new SolidColorBrush(Colors.Transparent);
            for (int i = 0; i < _thumbnailBorders.Count; i++)
                _thumbnailBorders[i].BorderBrush = (i + 1 == _pageCurrent) ? highlight : transparent;
        }

        // ── Parameter UI ─────────────────────────────────────────────

        private void BuildParameterUi()
        {
            ParameterItemsControl.Items.Clear();

            if (_report == null)
            {
                ParametersExpander.IsVisible = false;
                return;
            }

            var userParams = _report.UserReportParameters;
            bool hasVisible = false;

            foreach (UserReportParameter urp in userParams)
            {
                if (string.IsNullOrEmpty(urp.Prompt)) continue;
                hasVisible = true;

                var defaultStr = (urp.DefaultValue != null && urp.DefaultValue.Length > 0)
                    ? urp.DefaultValue[0]?.ToString() ?? string.Empty
                    : string.Empty;

                Control input;
                if (urp.DisplayValues is { Length: > 0 })
                {
                    var combo = new ComboBox
                    {
                        ItemsSource = urp.DisplayValues,
                        Width = 150,
                        Height = 28
                    };
                    var idx = Array.IndexOf(urp.DisplayValues, defaultStr);
                    combo.SelectedIndex = idx >= 0 ? idx : 0;
                    input = combo;
                }
                else if (urp.dt == TypeCode.Boolean)
                {
                    input = new CheckBox
                    {
                        IsChecked = defaultStr.Equals("true", StringComparison.OrdinalIgnoreCase)
                    };
                }
                else
                {
                    input = new TextBox
                    {
                        Text = defaultStr,
                        Width = 150,
                        Height = 28,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                }

                var paramPanel = new StackPanel { Spacing = 2, Tag = (urp.Name, input) };
                paramPanel.Children.Add(new TextBlock
                {
                    Text = urp.Prompt,
                    FontSize = 12,
                    Opacity = 0.7,
                    Margin = new Thickness(0, 0, 0, 2)
                });
                paramPanel.Children.Add(input);
                ParameterItemsControl.Items.Add(paramPanel);
            }

            ParametersExpander.IsVisible = hasVisible;
        }

        private void CollectParametersFromUi()
        {
            _parameters = new Dictionary<string, string>();

            foreach (var item in ParameterItemsControl.Items)
            {
                if (item is not StackPanel panel) continue;
                if (panel.Tag is not (string name, Control input)) continue;

                var value = input switch
                {
                    TextBox tb       => tb.Text ?? string.Empty,
                    ComboBox cb      => cb.SelectedItem?.ToString() ?? string.Empty,
                    CheckBox chk     => (chk.IsChecked ?? false).ToString().ToLowerInvariant(),
                    _                => string.Empty
                };
                ((Dictionary<string, string>)_parameters)[name] = value;
            }
        }

        // ── Panning (middle-mouse drag) ──────────────────────────────

        private void OnScrollViewerPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(ReportScrollViewer);
            if (point.Properties.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _panStart = point.Position;
                e.Pointer.Capture(ReportScrollViewer);
                e.Handled = true;
            }
        }

        private void OnScrollViewerPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning) return;
            var pos = e.GetCurrentPoint(ReportScrollViewer).Position;
            var delta = _panStart - pos;
            _panStart = pos;
            ReportScrollViewer.Offset += delta;
            e.Handled = true;
        }

        private void OnScrollViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isPanning && e.InitialPressMouseButton == MouseButton.Middle)
            {
                _isPanning = false;
                e.Pointer.Capture(null);
            }
        }

        private void OnScrollViewerPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _isPanning = false;
        }

        // ── Find / search ────────────────────────────────────────────

        private void OpenFindBar()
        {
            FindBar.IsVisible = true;
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }

        private void CloseFindBar()
        {
            FindBar.IsVisible = false;
            ReportCanvas.ClearSearch();
            _searchResults.Clear();
            _searchIndex = -1;
            _lastSearchText = string.Empty;
            FindStatusText.Text = string.Empty;
            FindTextBox.Text = string.Empty;
        }

        private void FindTextBoxOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNavigate(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : +1);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CloseFindBar();
                e.Handled = true;
            }
        }

        private void ExecuteSearch(string text)
        {
            if (text == _lastSearchText) return;
            _lastSearchText = text;
            _searchResults.Clear();
            _searchIndex = -1;

            if (string.IsNullOrEmpty(text) || _pages == null)
            {
                ReportCanvas.ClearSearch();
                FindStatusText.Text = string.Empty;
                return;
            }

            for (int i = 0; i < _pages.PageCount; i++)
            {
                var page = _pages[i];
                foreach (var obj in page)
                {
                    if (obj is PageText pt &&
                        pt.Text?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _searchResults.Add(new SearchMatch(i, pt));
                    }
                }
            }

            if (_searchResults.Count > 0)
            {
                _searchIndex = 0;
                ApplySearchMatch();
            }
            else
            {
                ReportCanvas.ClearSearch();
                FindStatusText.Text = "No matches";
            }
        }

        private void FindNavigate(int direction)
        {
            if (_searchResults.Count == 0)
            {
                ExecuteSearch(FindTextBox.Text ?? string.Empty);
                return;
            }

            _searchIndex = (_searchIndex + direction + _searchResults.Count) % _searchResults.Count;
            ApplySearchMatch();
        }

        private void ApplySearchMatch()
        {
            if (_searchIndex < 0 || _searchIndex >= _searchResults.Count)
                return;

            var match = _searchResults[_searchIndex];

            if (_pageCurrent - 1 != match.PageIndex)
                SetPage(match.PageIndex + 1);

            var pageItems = _searchResults
                .Where(r => r.PageIndex == match.PageIndex)
                .Select(r => r.Item);

            ReportCanvas.SetSearch(pageItems, match.Item);

            FindStatusText.Text = $"{_searchIndex + 1} of {_searchResults.Count}";
        }
    }
}

