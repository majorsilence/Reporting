using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Majorsilence.Forms;
using SkiaSharp;

namespace Majorsilence.Reporting.RdlViewer
{
    /// <summary>
    /// Colours of the viewer's and designer's own chrome -- the area around the page, rulers, bands,
    /// selection handles -- never report content. Resolved from the current Majorsilence.Forms theme on
    /// every call, so callers read them while painting and repaint on <see cref="Theme.ThemeChanged"/>.
    /// </summary>
    /// <remarks>
    /// Each colour is, in order: the <c>--rdl-*</c> author variable of the applied CSS theme (a theme
    /// sets e.g. <c>:root { --rdl-area-background: #202020; }</c>); on a dark theme, a colour derived from
    /// the Forms tokens; otherwise the classic colour, so the default look is unchanged.
    /// </remarks>
    public static class ChromeColors
    {
        /// <summary>
        /// Every <c>--rdl-*</c> variable the viewer and designer read. A theme declaring one gets a
        /// "declared but never used" warning from the CSS parser, which knows nothing of them;
        /// <see cref="IsKnownVariableWarning"/> recognises those.
        /// </summary>
        public static IReadOnlySet<string> Variables { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "--rdl-area-background",
            "--rdl-band-border",
            "--rdl-band-color",
            "--rdl-band-text",
            "--rdl-decorator",
            "--rdl-decorator-border",
            "--rdl-expression-hover",
            "--rdl-find-close-background",
            "--rdl-find-status",
            "--rdl-grid-dot",
            "--rdl-handle",
            "--rdl-highlight",
            "--rdl-highlight-all",
            "--rdl-hover-outline",
            "--rdl-item-outline",
            "--rdl-line-numbers",
            "--rdl-margin-color",
            "--rdl-out-of-page-color",
            "--rdl-page-edge",
            "--rdl-page-outline",
            "--rdl-ruler-background",
            "--rdl-ruler-gradient",
            "--rdl-ruler-text",
            "--rdl-selection-border",
            "--rdl-text",
            "--rdl-zoom-background",
        };

        /// <summary>
        /// True for the parser's "declared but never used" warning about one of <see cref="Variables"/>,
        /// which is not a problem here. A misspelt <c>--rdl-</c> name is not in the set and still warns.
        /// </summary>
        public static bool IsKnownVariableWarning(ThemeCssDiagnostic diagnostic)
        {
            if (diagnostic.Severity != ThemeCssSeverity.Warning || !diagnostic.Message.StartsWith("'--", StringComparison.Ordinal))
                return false;
            int end = diagnostic.Message.IndexOf('\'', 1);
            return end > 1
                && diagnostic.Message.IndexOf("never used", StringComparison.Ordinal) > 0
                && Variables.Contains(diagnostic.Message.Substring(1, end - 1));
        }

        /// <summary>True when the current theme's window background is dark.</summary>
        public static bool IsDarkTheme => Luminance(Theme.BackgroundColor) < 0.5f;

        /// <summary>
        /// The colour of the CSS variable <paramref name="variable"/> if the applied theme declares it;
        /// otherwise <paramref name="dark"/> on a dark theme (when given), else <paramref name="classic"/>.
        /// </summary>
        public static Color Resolve(string variable, Color classic, Func<Color> dark = null)
        {
            Debug.Assert(Variables.Contains(variable), $"{variable} is missing from ChromeColors.Variables");
            if (TryGetVariable(variable, out Color c))
                return c;
            if (dark != null && IsDarkTheme)
                return dark();
            return classic;
        }

        /// <summary>Converts a Forms theme token to a System.Drawing colour.</summary>
        public static Color FromToken(SKColor c) => Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);

        private static bool TryGetVariable(string variable, out Color color)
        {
            var sheets = Theme.CurrentStyleSheets;
            // the last sheet is the one applied; it wins over its bases
            for (int i = sheets.Count - 1; i >= 0; i--)
            {
                foreach (var v in sheets[i].Variables)
                {
                    if (!string.Equals(v.Name, variable, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var value = v.Value;
                    if (value != null && value.Kind == ThemeCssValueKind.Color)
                    {
                        color = Color.FromArgb(unchecked((int)value.Argb));
                        return true;
                    }
                }
            }
            color = Color.Empty;
            return false;
        }

        private static float Luminance(SKColor c) =>
            (0.2126f * c.Red + 0.7152f * c.Green + 0.0722f * c.Blue) / 255f;

        // ---- Viewer colours ----

        /// <summary>Outline and drop shadow of each page.</summary>
        public static Color PageOutline => Resolve("--rdl-page-outline", Color.Black, () => FromToken(Theme.BorderHighColor));

        /// <summary>Find: the current match.</summary>
        public static Color Highlight => Resolve("--rdl-highlight", Color.Aqua);

        /// <summary>Find: every other match when "highlight all" is on.</summary>
        public static Color HighlightAll => Resolve("--rdl-highlight-all", Color.Fuchsia);

        /// <summary>Background of the Find bar's close button.</summary>
        public static Color FindCloseBackground => Resolve("--rdl-find-close-background", Color.FromArgb(255, 192, 192), () => FromToken(Theme.WarningHighlightColor));

        /// <summary>The Find bar's "not found" status text.</summary>
        public static Color FindStatusText => Resolve("--rdl-find-status", Color.Salmon);

        /// <summary>Text drawn on the control itself, such as a render error.</summary>
        public static Color Text => Resolve("--rdl-text", Color.Black, () => FromToken(Theme.ForegroundColor));
    }
}
