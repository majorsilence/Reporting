using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Majorsilence.Reporting.Rdl;
using SkiaSharp;

namespace Majorsilence.Reporting.UI.RdlAvalonia.Viewer
{
    /// <summary>
    /// Represents an entry in the hit list for selection tracking
    /// </summary>
    public class HitListEntry
    {
        public Rect Rect { get; }
        public PageItem PageItem { get; }
        
        public HitListEntry(Rect rect, PageItem pageItem)
        {
            Rect = rect;
            PageItem = pageItem;
        }
        
        public bool Contains(Point p) => Rect.Contains(p);
    }
    
    public sealed class ReportCanvas : Control
    {
        private Pages? _pages;
        private int _pageIndex;
        private double _zoom = 1.0;
        private WriteableBitmap? _bitmap;
        private bool _needsRender = true;
        
        // Selection handling
        private readonly List<HitListEntry> _hitList = new();
        private readonly List<PageItem> _selectList = new();
        private bool _selectToolEnabled = true;
        private bool _isSelecting;
        private Point _selectionStart;
        private Point _selectionEnd;
        private readonly Color _selectionColor = Color.FromArgb(80, 51, 153, 255);
        private readonly Color _selectedItemColor = Color.FromArgb(100, 100, 149, 237);

        // Search highlighting
        private readonly List<PageItem> _searchPageItems = new();
        private PageItem? _currentSearchItem;
        private readonly Color _searchHighlightColor = Color.FromArgb(110, 255, 210, 0);
        private readonly Color _currentSearchHighlightColor = Color.FromArgb(200, 255, 140, 0);

        public ReportCanvas()
        {
            Focusable = true;
            Cursor = new Cursor(StandardCursorType.Ibeam);

            var copyItem = new MenuItem { Header = "Copy" };
            copyItem.Click += (_, _) => CopySelection();
            var selectAllItem = new MenuItem { Header = "Select All" };
            selectAllItem.Click += (_, _) => SelectAll();
            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(copyItem);
            ContextMenu.Items.Add(selectAllItem);
        }

        public void SetSearch(IEnumerable<PageItem> pageItems, PageItem? current)
        {
            _searchPageItems.Clear();
            _searchPageItems.AddRange(pageItems);
            _currentSearchItem = current;
            InvalidateVisual();
        }

        public void ClearSearch()
        {
            _searchPageItems.Clear();
            _currentSearchItem = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Event raised when the selection changes
        /// </summary>
        public event EventHandler? SelectionChanged;

        /// <summary>
        /// Gets or sets whether the selection tool is enabled
        /// </summary>
        public bool SelectToolEnabled
        {
            get => _selectToolEnabled;
            set
            {
                _selectToolEnabled = value;
                _selectList.Clear();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets whether there is any selected content that can be copied
        /// </summary>
        public bool CanCopy => _selectToolEnabled && _selectList.Count > 0;

        /// <summary>
        /// Gets the selected text content
        /// </summary>
        public string? SelectedText
        {
            get
            {
                if (!_selectToolEnabled || _selectList.Count == 0)
                    return null;

                var sb = new StringBuilder();
                float lastY = float.MinValue;

                var sortedList = _selectList
                    .OrderBy(pi => pi.Y)
                    .ThenBy(pi => pi.X)
                    .ToList();

                foreach (var pi in sortedList)
                {
                    if (pi is not PageText pt)
                        continue;
                    if (pt.HtmlParent != null)
                        continue;

                    if (lastY != float.MinValue)
                    {
                        if (Math.Abs(pt.Y - lastY) < 0.1f)
                            sb.Append('\t');
                        else
                            sb.Append(Environment.NewLine);
                    }

                    if (pt is PageTextHtml pth)
                        AppendHtmlText(pth, sb);
                    else
                        sb.Append(pt.Text);

                    lastY = pt.Y;
                }

                return sb.ToString();
            }
        }

        private static void AppendHtmlText(PageTextHtml pth, StringBuilder sb)
        {
            bool bFirst = true;
            float lastY = float.MaxValue;
            foreach (PageItem pi in pth)
            {
                if (bFirst)
                {
                    bFirst = false;
                    continue;
                }
                if (pi is not PageText pt)
                    continue;
                if (pt.Y > lastY)
                    sb.Append(' ');
                sb.Append(pt.Text);
                lastY = pt.Y;
            }
        }

        /// <summary>
        /// Copies the selected text to the clipboard
        /// </summary>
        public async void CopySelection()
        {
            var text = SelectedText;
            if (string.IsNullOrEmpty(text))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }

        /// <summary>
        /// Selects all text on the current page
        /// </summary>
        public void SelectAll()
        {
            _selectList.Clear();
            foreach (var entry in _hitList)
            {
                if (entry.PageItem is PageText && !_selectList.Contains(entry.PageItem))
                {
                    _selectList.Add(entry.PageItem);
                }
            }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        /// <summary>
        /// Clears the current selection
        /// </summary>
        public void ClearSelection()
        {
            _selectList.Clear();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        public void SetReport(Report? report, Pages? pages)
        {
            _pages = pages;
            _pageIndex = 0;
            _needsRender = true;
            _selectList.Clear();
            _hitList.Clear();
            InvalidateMeasure();
            InvalidateVisual();
        }

        public void SetPage(int pageIndex)
        {
            _pageIndex = Math.Max(0, pageIndex);
            _needsRender = true;
            _selectList.Clear();
            _hitList.Clear();
            InvalidateMeasure();
            InvalidateVisual();
        }

        public void SetZoom(double zoom)
        {
            _zoom = Math.Max(0.1, zoom);
            _needsRender = true;
            InvalidateMeasure();
            InvalidateVisual();
        }

        /// <summary>
        /// Gets the logical size of the current rendered page, used by the ScrollViewer.
        /// </summary>
        public Size PageLogicalSize
        {
            get
            {
                if (_pages == null || _pages.PageCount == 0)
                    return default;

                var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
                var dpi = 96.0 * scale;
                var width = _pages.PageWidth * dpi / 72.0 * _zoom / scale;
                var height = _pages.PageHeight * dpi / 72.0 * _zoom / scale;
                return new Size(width, height);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = PageLogicalSize;
            if (size.Width <= 0 || size.Height <= 0)
                return base.MeasureOverride(availableSize);
            return size;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            
            if (!_selectToolEnabled)
                return;

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                _isSelecting = true;
                _selectionStart = point.Position;
                _selectionEnd = point.Position;
                
                // Clear selection if Ctrl is not pressed
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    _selectList.Clear();
                }
                
                e.Handled = true;
                Focus();
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            var pos = e.GetPosition(this);

            if (!_isSelecting)
            {
                var hasLink = _hitList.Any(h => h.Contains(pos) && !string.IsNullOrEmpty(h.PageItem.HyperLink));
                Cursor = hasLink ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Ibeam);
            }

            if (!_selectToolEnabled || !_isSelecting)
                return;

            _selectionEnd = pos;
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!_selectToolEnabled || !_isSelecting)
                return;

            _isSelecting = false;
            _selectionEnd = e.GetPosition(this);

            var dragDelta = _selectionEnd - _selectionStart;
            bool isClick = Math.Abs(dragDelta.X) < 4 && Math.Abs(dragDelta.Y) < 4;

            if (isClick)
            {
                var linkEntry = _hitList.FirstOrDefault(
                    h => h.Contains(_selectionEnd) && !string.IsNullOrEmpty(h.PageItem.HyperLink));
                if (linkEntry != null)
                {
                    LaunchHyperLink(linkEntry.PageItem.HyperLink!);
                    return;
                }
            }

            var selectionRect = CreateRect(_selectionStart, _selectionEnd);
            bool ctrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            UpdateSelectionFromRect(selectionRect, ctrlPressed);

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        private async void LaunchHyperLink(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel != null)
                        await topLevel.Launcher.LaunchUriAsync(uri);
                }
            }
            catch { }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                switch (e.Key)
                {
                    case Key.C:
                        CopySelection();
                        e.Handled = true;
                        break;
                    case Key.A:
                        SelectAll();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Escape)
            {
                ClearSelection();
                e.Handled = true;
            }
        }

        private void UpdateSelectionFromRect(Rect selectionRect, bool ctrlPressed)
        {
            foreach (var entry in _hitList)
            {
                if (!entry.Rect.Intersects(selectionRect))
                    continue;

                bool inList = _selectList.Contains(entry.PageItem);
                if (ctrlPressed)
                {
                    if (inList)
                        _selectList.Remove(entry.PageItem);
                    else
                        _selectList.Add(entry.PageItem);
                }
                else
                {
                    if (!inList)
                        _selectList.Add(entry.PageItem);
                }
            }
        }

        private static Rect CreateRect(Point p1, Point p2)
        {
            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p2.X - p1.X);
            double height = Math.Abs(p2.Y - p1.Y);
            return new Rect(x, y, width, height);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_pages == null || _pages.PageCount == 0)
            {
                return;
            }

            if (_needsRender || _bitmap == null)
            {
                RenderPage();
            }

            if (_bitmap != null)
            {
                context.DrawImage(_bitmap, new Rect(0, 0, _bitmap.Size.Width, _bitmap.Size.Height));
            }

            DrawSearchHighlights(context);
            DrawSelectionHighlights(context);
            
            // Draw selection rectangle while dragging
            if (_isSelecting)
            {
                var selectionRect = CreateRect(_selectionStart, _selectionEnd);
                var brush = new SolidColorBrush(_selectionColor);
                var pen = new Pen(new SolidColorBrush(Color.FromRgb(51, 153, 255)));
                context.DrawRectangle(brush, pen, selectionRect);
            }
        }

        private void DrawSearchHighlights(DrawingContext context)
        {
            if (_searchPageItems.Count == 0)
                return;

            var highlightBrush = new SolidColorBrush(_searchHighlightColor);
            var currentBrush = new SolidColorBrush(_currentSearchHighlightColor);
            foreach (var entry in _hitList)
            {
                if (entry.PageItem == _currentSearchItem)
                    context.DrawRectangle(currentBrush, null, entry.Rect);
                else if (_searchPageItems.Contains(entry.PageItem))
                    context.DrawRectangle(highlightBrush, null, entry.Rect);
            }
        }

        private void DrawSelectionHighlights(DrawingContext context)
        {
            if (_selectList.Count == 0)
                return;

            var brush = new SolidColorBrush(_selectedItemColor);
            foreach (var entry in _hitList)
            {
                if (_selectList.Contains(entry.PageItem))
                {
                    context.DrawRectangle(brush, null, entry.Rect);
                }
            }
        }

        private void RenderPage()
        {
            if (_pages == null)
            {
                return;
            }

#if NET6_0_OR_GREATER
            var pageIndex = Math.Clamp(_pageIndex, 0, _pages.PageCount - 1);
#else
            var pageIndex = Math.Max(0, Math.Min(_pageIndex, _pages.PageCount - 1));
#endif
            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var dpi = 96.0 * scale;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(_pages.PageWidth * dpi / 72.0 * _zoom));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(_pages.PageHeight * dpi / 72.0 * _zoom));

            _bitmap = new WriteableBitmap(
                new PixelSize(pixelWidth, pixelHeight),
                new Vector(dpi, dpi),
                PixelFormats.Bgra8888,
                AlphaFormat.Premul);

            using var framebuffer = _bitmap.Lock();
            var info = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
            if (surface == null)
            {
                return;
            }

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            using var g = new Majorsilence.Drawing.Graphics(canvas)
            {
                DpiX = (float)dpi,
                DpiY = (float)dpi,
                PageUnit = Majorsilence.Drawing.GraphicsUnit.Pixel
            };

            // Items are in points; scale to physical pixels: points * (dpi/72) * zoom
            var effectiveZoom = (float)(_zoom * dpi / 72.0);
            var renderer = new SkiaPageDrawing(_pages, effectiveZoom);
            renderer.Draw(g, pageIndex);

            // Build hit list for selection
            BuildHitList(pageIndex);

            surface.Flush();
            _needsRender = false;
        }

        private void BuildHitList(int pageIndex)
        {
            _hitList.Clear();

            if (_pages == null || pageIndex < 0 || pageIndex >= _pages.PageCount)
                return;

            var page = _pages[pageIndex];
            BuildHitListFromPage(page);
        }

        private void BuildHitListFromPage(Majorsilence.Reporting.Rdl.Page page)
        {
            foreach (var item in page)
            {
                if (item is not PageItem pi)
                    continue;

                var rect = new Rect(
                    PointsToLogical(pi.X),
                    PointsToLogical(pi.Y),
                    PointsToLogical(pi.W),
                    PointsToLogical(pi.H)
                );

                if (pi is PageTextHtml pth)
                {
                    _hitList.Add(new HitListEntry(rect, pi));
                    foreach (PageItem child in pth)
                    {
                        if (child is PageText)
                        {
                            var childRect = new Rect(
                                PointsToLogical(child.X),
                                PointsToLogical(child.Y),
                                PointsToLogical(child.W),
                                PointsToLogical(child.H)
                            );
                            _hitList.Add(new HitListEntry(childRect, child));
                        }
                    }
                }
                else if (pi is PageText || pi is PageImage)
                {
                    _hitList.Add(new HitListEntry(rect, pi));
                }
            }
        }

        // Renderer places items at points * effectiveZoom physical pixels,
        // where effectiveZoom = _zoom * dpi/72 and dpi = 96 * renderScaling.
        // Logical position = physical / renderScaling = points * _zoom * 96/72.
        private double PointsToLogical(float points)
        {
            return points * _zoom * (96.0 / 72.0);
        }
    }
}
