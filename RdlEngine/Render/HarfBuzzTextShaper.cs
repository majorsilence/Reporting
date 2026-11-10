/*
 * Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#nullable disable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using Majorsilence.Pdf;
using HbBuffer = HarfBuzzSharp.Buffer;
using HbFont = HarfBuzzSharp.Font;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Shapes text for Majorsilence.Pdf with HarfBuzz: Arabic joining forms and ligatures, Indic
    /// reordering and conjuncts, mark positioning and kerning, from the font's own OpenType tables.
    /// Majorsilence.Pdf has no native dependency of its own; the engine, which already ships
    /// SkiaSharp's, plugs this in (RenderPdf_Raw).
    /// </summary>
    internal sealed class HarfBuzzTextShaper : ITextShaper
    {
        private static readonly Lazy<HarfBuzzTextShaper> _instance =
            new Lazy<HarfBuzzTextShaper>(Create, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The shaper, or null where HarfBuzz's native library cannot be loaded; PDF text is then
        /// drawn a glyph per character, in the order the bidirectional algorithm displays it.
        /// </summary>
        internal static HarfBuzzTextShaper Instance => _instance.Value;

        private static HarfBuzzTextShaper Create()
        {
            try
            {
                using (var probe = new HbBuffer())
                    probe.AddUtf16("a");
                return new HarfBuzzTextShaper();
            }
            catch (Exception ex) when (IsNativeLoadFailure(ex))
            {
                return null;
            }
        }

        private static bool IsNativeLoadFailure(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
                if (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException
                    || e is TypeInitializationException || e is PlatformNotSupportedException)
                    return true;
            return false;
        }

        private HarfBuzzTextShaper() { }

        // One HarfBuzz font per font: a TrueTypeFont is shared by documents and lives as long as
        // the font cache keeps it. HarfBuzz fonts can shape on several threads at once.
        private static readonly ConditionalWeakTable<TrueTypeFont, ShapingFont> _fonts =
            new ConditionalWeakTable<TrueTypeFont, ShapingFont>();

        // HarfBuzz counts references to its objects itself (a font holds its face, a face its blob),
        // so the wrappers are released by their own finalizers, in any order, when the font goes.
        private sealed class ShapingFont
        {
            internal readonly HbFont Font;
            private readonly Face _face;
            private readonly Blob _blob;

            internal ShapingFont(TrueTypeFont ttf)
            {
                byte[] data = ttf.Data;
                IntPtr copy = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, copy, data.Length);
                _blob = new Blob(copy, data.Length, MemoryMode.ReadOnly, () => Marshal.FreeHGlobal(copy));
                _face = new Face(_blob, ttf.FaceIndex);
                Font = new HbFont(_face);
                Font.SetFunctionsOpenType();
                // Positions come back in design units, the units ShapedGlyph is in.
                Font.SetScale(_face.UnitsPerEm, _face.UnitsPerEm);
            }
        }

        public ShapedGlyph[] Shape(TrueTypeFont font, string text, int start, int length, bool rightToLeft)
        {
            if (font == null || string.IsNullOrEmpty(text) || length <= 0)
                return Array.Empty<ShapedGlyph>();
            try
            {
                var shaping = _fonts.GetValue(font, f => new ShapingFont(f));
                using var buffer = new HbBuffer();
                // The whole line goes in as context, so that a letter at the edge of the range joins
                // as it does in the line; only the range is shaped.
                buffer.AddUtf16(text, start, length);
                buffer.Direction = rightToLeft ? Direction.RightToLeft : Direction.LeftToRight;
                buffer.GuessSegmentProperties();    // script and language; the direction stays as set
                shaping.Font.Shape(buffer);

                var infos = buffer.GlyphInfos;
                var positions = buffer.GlyphPositions;
                var glyphs = new ShapedGlyph[infos.Length];
                for (int i = 0; i < infos.Length; i++)
                {
                    glyphs[i] = new ShapedGlyph((ushort)infos[i].Codepoint, (int)infos[i].Cluster,
                        positions[i].XAdvance, positions[i].XOffset, positions[i].YOffset);
                }
                return glyphs;
            }
            catch (Exception ex) when (IsNativeLoadFailure(ex))
            {
                return null;     // the canvas draws the text from the font's cmap instead
            }
        }
    }
}
