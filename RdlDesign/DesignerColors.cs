using System.Drawing;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlViewer;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// The design surface's own colours, resolved from the current theme each time they are read (see
    /// <see cref="ChromeColors"/> for the order). Report content, including the white paper, is never themed.
    /// </summary>
    /// <remarks>
    /// Colours off the page (the area behind it, bands, rulers) follow a dark theme by themselves. Colours
    /// drawn on the white paper (margins, grid, selection, handles) keep their classic values unless the
    /// theme's CSS sets their <c>--rdl-*</c> variable, since the paper stays white.
    /// </remarks>
    internal static class DesignerColors
    {
        static Color Token(SkiaSharp.SKColor c) => ChromeColors.FromToken(c);

        // ---- Off the page ----

        /// <summary>Behind the page.</summary>
        internal static Color AreaBackground => ChromeColors.Resolve("--rdl-area-background", Color.LightGray, () => Token(Theme.ControlMidColor));

        /// <summary>Section bands (Body, PageHeader, PageFooter) and the ruler next to them.</summary>
        internal static Color Band => ChromeColors.Resolve("--rdl-band-color", Color.LightGray, () => Token(Theme.ControlMidHighColor));

        /// <summary>The band's caption ("Page Header ↑").</summary>
        internal static Color BandText => ChromeColors.Resolve("--rdl-band-text", Color.Black, () => Token(Theme.ForegroundColor));

        internal static Color BandBorder => ChromeColors.Resolve("--rdl-band-border", Color.DimGray, () => Token(Theme.BorderMidColor));

        /// <summary>The right-hand edge of the page.</summary>
        internal static Color PageEdge => ChromeColors.Resolve("--rdl-page-edge", Color.Gray, () => Token(Theme.BorderHighColor));

        internal static Color RulerBackground => ChromeColors.Resolve("--rdl-ruler-background", Color.White, () => Token(Theme.ControlLowColor));

        /// <summary>The far end of the ruler's gradient; translucent.</summary>
        internal static Color RulerGradient => ChromeColors.Resolve("--rdl-ruler-gradient", Color.FromArgb(30, Color.LightSkyBlue), () => Color.FromArgb(30, Token(Theme.AccentColor)));

        internal static Color RulerText => ChromeColors.Resolve("--rdl-ruler-text", Color.Black, () => Token(Theme.ForegroundColor));

        /// <summary>Text drawn on the design surface itself, such as a parse error.</summary>
        internal static Color Text => ChromeColors.Text;

        /// <summary>Line numbers next to the RDL text editor.</summary>
        internal static Color LineNumbers => ChromeColors.Resolve("--rdl-line-numbers", Color.DarkBlue, () => Token(Theme.ForegroundDisabledColor));

        /// <summary>The toolbar's zoom box.</summary>
        internal static Color ZoomBackground => ChromeColors.Resolve("--rdl-zoom-background", Color.White, () => Token(Theme.ControlLowColor));

        /// <summary>The toolbar's "fx" expression button while hovered.</summary>
        internal static Color ExpressionHover => ChromeColors.Resolve("--rdl-expression-hover", Color.LightGray, () => Token(Theme.ControlHighlightLowColor));

        // ---- On the page ----

        /// <summary>The page's margins, outside the work area.</summary>
        internal static Color Margin => ChromeColors.Resolve("--rdl-margin-color", Color.Azure);

        /// <summary>Parts of an item that fall outside the work area.</summary>
        internal static Color OutOfPage => ChromeColors.Resolve("--rdl-out-of-page-color", Color.Salmon);

        internal static Color GridDot => ChromeColors.Resolve("--rdl-grid-dot", Color.FromArgb(0xA0, 0xA0, 0xA0));

        internal static Color HoverOutline => ChromeColors.Resolve("--rdl-hover-outline", Color.FromArgb(0x4A, 0x90, 0xD9));

        internal static Color SelectionBorder => ChromeColors.Resolve("--rdl-selection-border", Color.DimGray);

        /// <summary>Selection, resize and table handles.</summary>
        internal static Color Handle => ChromeColors.Resolve("--rdl-handle", Color.Black);

        /// <summary>Outline of an item that has no border of its own.</summary>
        internal static Color ItemOutline => ChromeColors.Resolve("--rdl-item-outline", Color.LightGray);

        /// <summary>Table row/column/table selection decorator (issue #293).</summary>
        internal static Color Decorator => ChromeColors.Resolve("--rdl-decorator", Color.FromArgb(0xD6, 0xE4, 0xF5));

        internal static Color DecoratorBorder => ChromeColors.Resolve("--rdl-decorator-border", Color.FromArgb(0x4A, 0x90, 0xD9));
    }
}
