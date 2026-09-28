// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using Majorsilence.Pdf;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;
using UglyToad.PdfPig.Content;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class FontRegistryTests
    {
        // ── installed fonts, by the family each declares ─────────────────────
        //
        // A report names a font by its family - "Impact", "MICR Encoding" - and the file on
        // disk can be called anything. Registering only a fixed list meant every other family
        // rendered as Arial, a cheque's MICR line and a barcode font included.

        private static string NewFontDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "fontscan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string DeclaredFamily(string path)
        {
            Assert.That(InstalledFontScanner.TryRead(path, out var face), Is.True, path);
            return face.Family;
        }

        [Test]
        public void AddInstalledFonts_FindsAFontByTheFamilyItDeclares_NotItsFileName()
        {
            var regular = SystemSansRegular;
            Assume.That(regular, Is.Not.Null, "no system sans font on this machine");
            string family = DeclaredFamily(regular!);

            string dir = NewFontDir();
            string copy = Path.Combine(dir, "zz_nothing_like_the_name.ttf");
            File.Copy(regular!, copy);

            var reg = new FontRegistry().AddInstalledFonts(dir);

            Assert.That(reg.Contains(family), Is.True, $"registered as \"{family}\"");
            Assert.That(reg.Resolve(family, false, false)?.Path, Is.EqualTo(copy));
        }

        [Test]
        public void AddInstalledFonts_PlacesEachFileByItsOwnBoldAndItalicBits()
        {
            var regular = SystemSansRegular;
            var bold = SystemSansBold;
            Assume.That(regular, Is.Not.Null);
            Assume.That(bold, Is.Not.Null);
            string family = DeclaredFamily(regular!);
            Assume.That(DeclaredFamily(bold!), Is.EqualTo(family), "regular and bold are one family");

            // Names that sort the bold file first and say nothing about which is which.
            string dir = NewFontDir();
            string boldCopy = Path.Combine(dir, "a.ttf");
            string regularCopy = Path.Combine(dir, "b.ttf");
            File.Copy(bold!, boldCopy);
            File.Copy(regular!, regularCopy);

            var reg = new FontRegistry().AddInstalledFonts(dir);

            Assert.Multiple(() =>
            {
                Assert.That(reg.Resolve(family, false, false)?.Path, Is.EqualTo(regularCopy));
                Assert.That(reg.Resolve(family, true, false)?.Path, Is.EqualTo(boldCopy));
            });
        }

        [Test]
        public void AddInstalledFonts_LeavesAnExplicitlyRegisteredFamilyAlone()
        {
            var regular = SystemSansRegular;
            var bold = SystemSansBold;
            Assume.That(regular, Is.Not.Null);
            Assume.That(bold, Is.Not.Null);
            string family = DeclaredFamily(regular!);

            string dir = NewFontDir();
            File.Copy(regular!, Path.Combine(dir, "installed.ttf"));

            // Registered first, deliberately pointing somewhere else.
            var reg = new FontRegistry().AddFamily(family, regular: bold!).AddInstalledFonts(dir);

            Assert.That(reg.Resolve(family, false, false)?.Path, Is.EqualTo(bold));
        }

        [Test]
        public void AddInstalledFonts_SkipsACollectionItCannotEmbed()
        {
            string? ttc = FindFont(@"C:\Windows\Fonts\cambria.ttc", @"C:\Windows\Fonts\msgothic.ttc");
            Assume.That(ttc, Is.Not.Null, "no font collection on this machine");

            // Named .ttf, so only the file's own header can give it away.
            string dir = NewFontDir();
            string copy = Path.Combine(dir, "collection.ttf");
            File.Copy(ttc!, copy);

            Assert.That(InstalledFontScanner.TryRead(copy, out _), Is.False);
            Assert.That(InstalledFontScanner.Scan(dir), Is.Empty);
        }

        [Test]
        public void AddInstalledFonts_AMissingDirectoryAddsNothing()
        {
            var reg = new FontRegistry().AddInstalledFonts(
                Path.Combine(Path.GetTempPath(), "no-such-font-dir-" + Guid.NewGuid().ToString("N")));
            Assert.That(reg.Contains("Arial"), Is.False);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static string? FindFont(params string[] paths)
        {
            foreach (var p in paths) if (File.Exists(p)) return p;
            return null;
        }

        private static string? SystemSansRegular => FindFont(
            @"C:\Windows\Fonts\arial.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/Library/Fonts/Arial.ttf");

        private static string? SystemSansBold => FindFont(
            @"C:\Windows\Fonts\arialbd.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
            "/Library/Fonts/Arial Bold.ttf");

        // The fallback fonts ship embedded in Majorsilence.Forms.Drawing.Common; the loader
        // extracts them to a temp directory on first use.
        private static string BundledFontsDir =>
            Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();

        private static bool BundledFontsExist => Directory.Exists(BundledFontsDir);

        private static void AssertValidPdf(byte[] bytes)
        {
            Assert.That(bytes, Is.Not.Null.And.Not.Empty);
            using var doc = PdfPig.Open(bytes);
            Assert.That(doc.NumberOfPages, Is.GreaterThan(0));
        }

        // ── FontSource ───────────────────────────────────────────────────────

        [Test]
        public void FontSource_FromPath_StoresProperly()
        {
            var src = FontSource.FromPath(@"Fonts\arial.ttf");
            Assert.That(src.Path, Is.EqualTo(@"Fonts\arial.ttf"));
            Assert.That(src.Data, Is.Null);
            Assert.That(src.CacheKey, Is.EqualTo(@"Fonts\arial.ttf"));
        }

        [Test]
        public void FontSource_FromBytes_StoresProperly()
        {
            var data = new byte[] { 1, 2, 3 };
            var src = FontSource.FromBytes("MyFont-Regular", data);
            Assert.That(src.Path, Is.Null);
            Assert.That(src.Data, Is.SameAs(data));
            Assert.That(src.CacheKey, Is.EqualTo("MyFont-Regular"));
        }

        [Test]
        public void FontSource_FromPath_NullPath_Throws() =>
            Assert.Throws<ArgumentNullException>(() => FontSource.FromPath(null!));

        [Test]
        public void FontSource_FromBytes_NullData_Throws() =>
            Assert.Throws<ArgumentNullException>(() => FontSource.FromBytes("key", null!));

        // ── FontRegistry.AddFamily ────────────────────────────────────────────

        [Test]
        public void AddFamily_ByPath_Resolves()
        {
            var reg = new FontRegistry()
                .AddFamily("MyFont",
                    regular: @"Fonts\regular.ttf",
                    bold:    @"Fonts\bold.ttf");

            var r = reg.Resolve("MyFont", bold: false, italic: false);
            Assert.That(r, Is.Not.Null);
            Assert.That(r!.Path, Is.EqualTo(@"Fonts\regular.ttf"));

            var b = reg.Resolve("MyFont", bold: true, italic: false);
            Assert.That(b!.Path, Is.EqualTo(@"Fonts\bold.ttf"));
        }

        [Test]
        public void AddFamily_ByBytes_Resolves()
        {
            var data = new byte[16];
            var reg = new FontRegistry()
                .AddFamily("MyFont", regular: data);

            var r = reg.Resolve("MyFont", bold: false, italic: false);
            Assert.That(r, Is.Not.Null);
            Assert.That(r!.Data, Is.SameAs(data));
        }

        [Test]
        public void Resolve_UnknownFamily_ReturnsNull()
        {
            var reg = new FontRegistry();
            Assert.That(reg.Resolve("DoesNotExist", false, false), Is.Null);
        }

        [Test]
        public void Resolve_FallsBackToRegular_WhenBoldMissing()
        {
            var reg = new FontRegistry()
                .AddFamily("MyFont", regular: @"regular.ttf");

            var r = reg.Resolve("MyFont", bold: true, italic: false);
            Assert.That(r!.Path, Is.EqualTo("regular.ttf"));
        }

        [Test]
        public void Contains_ReturnsTrueForRegisteredFamily()
        {
            var reg = new FontRegistry().AddFamily("Foo", regular: "foo.ttf");
            Assert.That(reg.Contains("Foo"), Is.True);
            Assert.That(reg.Contains("Bar"), Is.False);
        }

        [Test]
        public void Contains_IsCaseInsensitive()
        {
            var reg = new FontRegistry().AddFamily("Arial", regular: "arial.ttf");
            Assert.That(reg.Contains("arial"), Is.True);
            Assert.That(reg.Contains("ARIAL"), Is.True);
        }

        // ── AddDirectory ──────────────────────────────────────────────────────

        [Test]
        public void AddDirectory_NonExistentDir_Throws()
        {
            var reg = new FontRegistry();
            Assert.Throws<DirectoryNotFoundException>(() =>
                reg.AddDirectory(@"C:\DoesNotExist\Fonts"));
        }

        [Test]
        public void AddDirectory_DetectsBundledFamilies()
        {
            Assume.That(BundledFontsExist, "Bundled fonts directory not found");

            var reg = new FontRegistry().AddDirectory(BundledFontsDir);

            Assert.That(reg.Contains("LiberationSans"),  Is.True, "LiberationSans");
            Assert.That(reg.Contains("LiberationSerif"), Is.True, "LiberationSerif");
            Assert.That(reg.Contains("LiberationMono"),  Is.True, "LiberationMono");
            Assert.That(reg.Contains("NotoSans"),        Is.True, "NotoSans");
            Assert.That(reg.Contains("Caladea"),         Is.True, "Caladea");
            Assert.That(reg.Contains("Carlito"),         Is.True, "Carlito");
        }

        [Test]
        public void AddDirectory_ResolvesVariants()
        {
            Assume.That(BundledFontsExist, "Bundled fonts directory not found");

            var reg = new FontRegistry().AddDirectory(BundledFontsDir);

            var r  = reg.Resolve("LiberationSans", bold: false, italic: false);
            var b  = reg.Resolve("LiberationSans", bold: true,  italic: false);
            var i  = reg.Resolve("LiberationSans", bold: false, italic: true);
            var bi = reg.Resolve("LiberationSans", bold: true,  italic: true);

            Assert.That(r,  Is.Not.Null, "regular");
            Assert.That(b,  Is.Not.Null, "bold");
            Assert.That(i,  Is.Not.Null, "italic");
            Assert.That(bi, Is.Not.Null, "bold-italic");

            Assert.That(r!.Path,  Does.EndWith("Regular.ttf").IgnoreCase);
            Assert.That(b!.Path,  Does.EndWith("Bold.ttf").IgnoreCase);
            Assert.That(i!.Path,  Does.EndWith("Italic.ttf").IgnoreCase);
            Assert.That(bi!.Path, Does.EndWith("BoldItalic.ttf").IgnoreCase);
        }

        // ── fallback chain ────────────────────────────────────────────────────

        [Test]
        public void AddFallback_ReturnsOrderedSources()
        {
            var reg = new FontRegistry()
                .AddFamily("A", regular: "a.ttf")
                .AddFamily("B", regular: "b.ttf")
                .AddFallback("A")
                .AddFallback("B");

            var fb = reg.GetFallbackSources(bold: false, italic: false);
            Assert.That(fb.Count, Is.EqualTo(2));
            Assert.That(fb[0].Path, Is.EqualTo("a.ttf"));
            Assert.That(fb[1].Path, Is.EqualTo("b.ttf"));
        }

        [Test]
        public void AddFallback_SkipsUnregisteredFamily()
        {
            var reg = new FontRegistry()
                .AddFamily("A", regular: "a.ttf")
                .AddFallback("A")
                .AddFallback("Missing");

            var fb = reg.GetFallbackSources(false, false);
            Assert.That(fb.Count, Is.EqualTo(1));
        }

        // ── PDF rendering with registry ───────────────────────────────────────

        [Test]
        public void PdfDocument_WithFontRegistry_RendersText()
        {
            Assume.That(SystemSansRegular, Is.Not.Null, "No system sans-serif font found");

            var registry = new FontRegistry()
                .AddFamily("TestSans",
                    regular: SystemSansRegular,
                    bold:    SystemSansBold ?? SystemSansRegular);

            var bytes = PdfDocument.Create()
                .WithFontRegistry(registry)
                .AddPage(PageSizes.A4, canvas =>
                {
                    var style = TextStyle.Default.WithFamily("TestSans").WithSize(14);
                    canvas.DrawText("Hello from FontRegistry!", 72, 100, style);
                    canvas.DrawText("Bold variant", 72, 130, style.WithBold());
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void PdfDocument_WithRegistry_BundledFonts_ProducesValidPdf()
        {
            Assume.That(BundledFontsExist, "Bundled fonts directory not found");

            var registry = new FontRegistry()
                .AddDirectory(BundledFontsDir)
                .AddFallback("NotoSans");

            var bytes = PdfDocument.Create()
                .WithFontRegistry(registry)
                .AddPage(PageSizes.A4, canvas =>
                {
                    float y = 60;
                    foreach (var family in new[] { "LiberationSans", "LiberationSerif",
                                                   "LiberationMono", "NotoSans", "Caladea", "Carlito" })
                    {
                        canvas.DrawText($"{family}: The quick brown fox.", 72, y,
                            TextStyle.Default.WithFamily(family).WithSize(12));
                        y += 20;
                    }
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void PdfDocument_FallbackRendering_DoesNotThrow()
        {
            Assume.That(BundledFontsExist, "Bundled fonts directory not found");

            // LiberationSans is Latin-only; NotoSans has broader coverage.
            // The text mixes Latin and extended chars — fallback kicks in for missing glyphs.
            var registry = new FontRegistry()
                .AddDirectory(BundledFontsDir)
                .AddFallback("NotoSans");

            var bytes = PdfDocument.Create()
                .WithFontRegistry(registry)
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("café résumé naïve — em dash", 72, 100,
                        TextStyle.Default.WithFamily("LiberationSans").WithSize(14)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void PdfDocument_RegistryFamily_MeasureTextWidth_UsesActualFont()
        {
            Assume.That(SystemSansRegular, Is.Not.Null, "No system sans-serif font found");

            var registry = new FontRegistry()
                .AddFamily("TestSans", regular: SystemSansRegular);

            var style = TextStyle.Default.WithFamily("TestSans").WithSize(12);
            float width = 0;

            PdfDocument.Create()
                .WithFontRegistry(registry)
                .AddPage(PageSizes.A4, canvas =>
                {
                    width = canvas.MeasureTextWidth("Hello, World!", style);
                    canvas.DrawText("Hello, World!", 72, 100, style);
                })
                .ToBytes();

            Assert.That(width, Is.GreaterThan(0));
            Assert.That(width, Is.LessThan(200)); // sanity: "Hello, World!" at 12pt
        }

        [Test]
        public void PdfDocument_NoRegistry_StandardFont_WinAnsiChars_StillWork()
        {
            // Ensure the non-registry path (standard Helvetica) still works for WinAnsi text
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Normal — em dash – en dash", 72, 100,
                        TextStyle.Default.WithSize(12)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void PdfDocument_WithRegistry_Underline_UsesCorrectWidth()
        {
            Assume.That(SystemSansRegular, Is.Not.Null, "No system font found");

            var registry = new FontRegistry()
                .AddFamily("TestSans", regular: SystemSansRegular);

            var bytes = PdfDocument.Create()
                .WithFontRegistry(registry)
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Underlined text", 72, 100,
                        TextStyle.Default.WithFamily("TestSans").WithSize(14).WithUnderline()))
                .ToBytes();

            AssertValidPdf(bytes);
        }
    }
}
