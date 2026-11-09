// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using Majorsilence.Reporting.RdlViewer;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
    /// <summary>
    /// Syntax colours of the SQL and XML editors (#527), resolved from the current theme each time
    /// the text is coloured (see <see cref="ChromeColors.Resolve"/>): a theme's <c>--rdl-syntax-*</c>
    /// variable, else a lighter shade on a dark theme, else the classic colour.
    /// </summary>
    internal static class SyntaxColors
    {
        // The classic colour differs between the editors for some kinds (a SQL string is red, an XML
        // one deep pink), so each takes it from the caller.
        internal static Color Comment(Color classic) => ChromeColors.Resolve("--rdl-syntax-comment", classic, () => Color.FromArgb(0x6A, 0x99, 0x55));
        internal static Color Keyword(Color classic) => ChromeColors.Resolve("--rdl-syntax-keyword", classic, () => Color.FromArgb(0x56, 0x9C, 0xD6));
        internal static Color Logical(Color classic) => ChromeColors.Resolve("--rdl-syntax-logical", classic, () => Color.FromArgb(0xA0, 0xA0, 0xA8));
        internal static Color Function(Color classic) => ChromeColors.Resolve("--rdl-syntax-function", classic, () => Color.FromArgb(0xC5, 0x86, 0xC0));
        internal static Color SystemName(Color classic) => ChromeColors.Resolve("--rdl-syntax-system", classic, () => Color.FromArgb(0x4E, 0xC9, 0xB0));
        internal static Color Number(Color classic) => ChromeColors.Resolve("--rdl-syntax-number", classic, () => Color.FromArgb(0xB5, 0xCE, 0xA8));
        internal static Color String(Color classic) => ChromeColors.Resolve("--rdl-syntax-string", classic, () => Color.FromArgb(0xCE, 0x91, 0x78));
        internal static Color Operator(Color classic) => ChromeColors.Resolve("--rdl-syntax-operator", classic, () => ChromeColors.Text);
        internal static Color Tag(Color classic) => ChromeColors.Resolve("--rdl-syntax-tag", classic, () => Color.FromArgb(0x56, 0x9C, 0xD6));
        internal static Color Attribute(Color classic) => ChromeColors.Resolve("--rdl-syntax-attribute", classic, () => Color.FromArgb(0x9C, 0xDC, 0xFE));
        internal static Color Entity(Color classic) => ChromeColors.Resolve("--rdl-syntax-entity", classic, () => Color.FromArgb(0xD7, 0xBA, 0x7D));
    }
}
