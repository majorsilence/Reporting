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
        public static DesignerThemeResult Apply(string theme, string cssPath) => ApplyCore(null, theme, cssPath);

        /// <summary>
        /// As <see cref="Apply"/>, but into <paramref name="scope"/> rather than the application theme --
        /// for one designer or viewer among others (<c>control.ThemeScope = scope</c>).
        /// </summary>
        public static DesignerThemeResult ApplyTo(ThemeScope scope, string theme, string cssPath)
        {
            ArgumentNullException.ThrowIfNull(scope);
            return ApplyCore(scope, theme, cssPath);
        }

        // scope null: the application theme
        private static DesignerThemeResult ApplyCore(ThemeScope scope, string theme, string cssPath)
        {
            if (string.IsNullOrWhiteSpace(theme) || string.Equals(theme, Custom, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(cssPath))
                theme = nameof(BuiltInTheme.Default);

            if (!string.Equals(theme, Custom, StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse(theme, true, out BuiltInTheme builtIn))
                    return FallBack(scope, new List<string> { $"Unknown theme '{theme}'." });
                SetBuiltIn(scope, builtIn);
                return new DesignerThemeResult(Array.Empty<string>(), false);
            }

            string css;
            try
            {
                css = File.ReadAllText(cssPath);
            }
            catch (Exception ex)
            {
                return FallBack(scope, new List<string> { $"{cssPath}: {ex.Message}" });
            }

            return ApplyCssCore(scope, css, cssPath);
        }

        /// <summary>Applies CSS theme text; see <see cref="Apply"/> for the fallback.</summary>
        public static DesignerThemeResult ApplyCss(string css, string sourceName = null) => ApplyCssCore(null, css, sourceName);

        /// <summary>As <see cref="ApplyCss"/>, but into <paramref name="scope"/>; see <see cref="ApplyTo"/>.</summary>
        public static DesignerThemeResult ApplyCssTo(ThemeScope scope, string css, string sourceName = null)
        {
            ArgumentNullException.ThrowIfNull(scope);
            return ApplyCssCore(scope, css, sourceName);
        }

        private static DesignerThemeResult ApplyCssCore(ThemeScope scope, string css, string sourceName)
        {
            var sheet = ThemeStyleSheet.Parse(css ?? "");
            string prefix = string.IsNullOrEmpty(sourceName) ? "" : Path.GetFileName(sourceName) + " ";
            var messages = sheet.Diagnostics
                .Where(d => !RdlViewer.ChromeColors.IsKnownVariableWarning(d))
                .Select(d => prefix + d).ToList();

            if (sheet.HasErrors)
                return FallBack(scope, messages);

            // a sheet without "extends" layers onto whatever is current; start it from Default so
            // re-applying the same file always gives the same result
            if (scope == null)
            {
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
            }
            else
            {
                scope.BeginUpdate();
                try
                {
                    scope.SetBuiltInTheme(BuiltInTheme.Default);
                    scope.ApplyStyleSheet(sheet);
                }
                finally
                {
                    scope.EndUpdate();
                }
            }
            return new DesignerThemeResult(messages, false);
        }

        private static void SetBuiltIn(ThemeScope scope, BuiltInTheme theme)
        {
            if (scope == null)
                Theme.SetBuiltInTheme(theme);
            else
                scope.SetBuiltInTheme(theme);
        }

        private static DesignerThemeResult FallBack(ThemeScope scope, List<string> messages)
        {
            SetBuiltIn(scope, BuiltInTheme.Default);
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
