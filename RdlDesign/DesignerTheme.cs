using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Applies the designer's theme: one of the Majorsilence.Forms built-in themes, or a CSS theme file
    /// (see Majorsilence.Forms docs/theming.md; the designer also reads <c>--rdl-*</c> variables).
    /// </summary>
    public static class DesignerTheme
    {
        /// <summary>The setting value meaning "a CSS file", whose path is stored alongside.</summary>
        public const string Custom = "Custom";

        /// <summary>The built-in themes offered in Options, in display order.</summary>
        public static IReadOnlyList<string> BuiltInNames { get; } = new[]
        {
            nameof(BuiltInTheme.Default), nameof(BuiltInTheme.Light), nameof(BuiltInTheme.Dark),
            nameof(BuiltInTheme.Classic), nameof(BuiltInTheme.Aero),
        };

        /// <summary>The folder next to the executable whose <c>*.css</c> files Options lists.</summary>
        public static string ThemesDirectory => Path.Combine(AppContext.BaseDirectory, "Themes");

        /// <summary>The <c>*.css</c> files shipped in <see cref="ThemesDirectory"/>.</summary>
        public static IReadOnlyList<string> ShippedThemeFiles()
        {
            try
            {
                return Directory.Exists(ThemesDirectory)
                    ? Directory.GetFiles(ThemesDirectory, "*.css").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray()
                    : Array.Empty<string>();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Applies <paramref name="theme"/> (a built-in name, or <see cref="Custom"/> with
        /// <paramref name="cssPath"/>). Never throws for a bad theme: an unknown name, a missing file or a
        /// stylesheet with errors falls back to <see cref="BuiltInTheme.Default"/>.
        /// </summary>
        public static DesignerThemeResult Apply(string theme, string cssPath)
        {
            if (string.IsNullOrWhiteSpace(theme) || string.Equals(theme, Custom, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(cssPath))
                theme = nameof(BuiltInTheme.Default);

            if (!string.Equals(theme, Custom, StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse(theme, true, out BuiltInTheme builtIn))
                    return FallBack(new List<string> { $"Unknown theme '{theme}'." });
                Theme.SetBuiltInTheme(builtIn);
                return new DesignerThemeResult(Array.Empty<string>(), false);
            }

            string css;
            try
            {
                css = File.ReadAllText(cssPath);
            }
            catch (Exception ex)
            {
                return FallBack(new List<string> { $"{cssPath}: {ex.Message}" });
            }

            return ApplyCss(css, cssPath);
        }

        /// <summary>Applies CSS theme text; see <see cref="Apply"/> for the fallback.</summary>
        public static DesignerThemeResult ApplyCss(string css, string sourceName = null)
        {
            var sheet = ThemeStyleSheet.Parse(css ?? "");
            string prefix = string.IsNullOrEmpty(sourceName) ? "" : Path.GetFileName(sourceName) + " ";
            var messages = sheet.Diagnostics
                .Where(d => !RdlViewer.ChromeColors.IsKnownVariableWarning(d))
                .Select(d => prefix + d).ToList();

            if (sheet.HasErrors)
                return FallBack(messages);

            // a sheet without "extends" layers onto whatever is current; start it from Default so
            // re-applying the same file always gives the same result
            Theme.BeginUpdate();
            try
            {
                Theme.SetBuiltInTheme(BuiltInTheme.Default);
                Theme.ApplyStyleSheet(sheet);
            }
            finally
            {
                Theme.EndUpdate();
            }
            return new DesignerThemeResult(messages, false);
        }

        private static DesignerThemeResult FallBack(List<string> messages)
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Default);
            messages.Add("Using the Default theme instead.");
            return new DesignerThemeResult(messages, true);
        }
    }

    /// <summary>The outcome of <see cref="DesignerTheme.Apply"/>.</summary>
    public sealed class DesignerThemeResult
    {
        internal DesignerThemeResult(IReadOnlyList<string> messages, bool fellBack)
        {
            Messages = messages;
            FellBack = fellBack;
        }

        /// <summary>Problems found, one line each (CSS warnings included); empty when there were none.</summary>
        public IReadOnlyList<string> Messages { get; }

        /// <summary>True when the theme could not be used and Default was applied instead.</summary>
        public bool FellBack { get; }
    }
}
