// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;

namespace Majorsilence.Pdf.Tests
{
    /// <summary>
    /// CFF-outline (OpenType "OTTO") fonts, such as Noto Sans CJK, which is the default CJK font on
    /// most Linux systems. The subsetter is tested on a hand-built CFF so the tests run anywhere; the
    /// end-to-end tests use the real Noto Sans CJK where it is installed.
    /// </summary>
    [TestFixture]
    public class CffFontTests
    {
        // ── a small name-keyed CFF built by hand ─────────────────────────────

        private static readonly byte[] Body = { 139, 139, 21, 11 };      // 0 0 rmoveto return
        private static readonly byte[] Endchar = { 14 };
        private static readonly byte[] Return = { 11 };

        private static byte[] Index(params byte[][] items)
        {
            var o = new MemoryStream();
            o.WriteByte((byte)(items.Length >> 8)); o.WriteByte((byte)items.Length);
            if (items.Length == 0) return o.ToArray();
            o.WriteByte(2); // offSize
            int cur = 1;
            void Off(int v) { o.WriteByte((byte)(v >> 8)); o.WriteByte((byte)v); }
            Off(cur);
            foreach (var it in items) { cur += it.Length; Off(cur); }
            foreach (var it in items) o.Write(it, 0, it.Length);
            return o.ToArray();
        }

        private static byte[] Int5(int v) => new byte[] { 29, (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };

        // Glyphs: 0 .notdef; 1 calls local 0 (which calls global 1); 2 calls global 0;
        // 3 calls local 1; 4 has a hintmask whose mask byte looks like a number, then calls local 2.
        private static byte[] BuildNameKeyedCff()
        {
            byte[][] glyphs =
            {
                Endchar,
                new byte[] { 32, 10, 14 },                  // -107 callsubr (local 0)
                new byte[] { 32, 29, 14 },                  // -107 callgsubr (global 0)
                new byte[] { 33, 10, 14 },                  // -106 callsubr (local 1)
                new byte[] { 149, 159, 18, 19, 0x20, 34, 10, 14 }, // 10 20 hstemhm hintmask <mask> -105 callsubr (local 2)
            };
            byte[][] locals = { new byte[] { 33, 29, 11 }, Body, Body };   // local 0 calls global 1
            byte[][] globals = { Body, Body };

            byte[] header = { 1, 0, 4, 2 };
            byte[] names = Index(Encoding.ASCII.GetBytes("Test"));
            byte[] strings = Index();
            byte[] gsubrs = Index(globals);
            byte[] cs = Index(glyphs);
            byte[] lsubrs = Index(locals);

            // Top DICT: CharStrings (17) and Private (18), every operand a five-byte integer.
            byte[] TopDict(int csOff, int privSize, int privOff) =>
                Int5(csOff).Concat(new byte[] { 17 }).Concat(Int5(privSize)).Concat(Int5(privOff)).Concat(new byte[] { 18 }).ToArray();

            int topIndexLen = Index(TopDict(0, 0, 0)).Length;
            int csOffset = header.Length + names.Length + topIndexLen + strings.Length + gsubrs.Length;
            byte[] priv = new byte[0];
            // Private DICT: Subrs (19) offset, relative to the Private DICT, is its own length (6).
            priv = Int5(6).Concat(new byte[] { 19 }).ToArray();
            int privOffset = csOffset + cs.Length;
            byte[] top = TopDict(csOffset, priv.Length, privOffset);

            return header.Concat(names).Concat(Index(top)).Concat(strings).Concat(gsubrs)
                .Concat(cs).Concat(priv).Concat(lsubrs).ToArray();
        }

        [Test]
        public void Subset_KeepsUsedGlyphsAndTheSubroutinesTheyReach()
        {
            var cff = BuildNameKeyedCff();
            var original = CffSubsetter.Read(cff);
            Assert.That(original.CharStrings, Has.Length.EqualTo(5));
            Assert.That(original.LocalSubrs[0], Has.Length.EqualTo(3));

            var sub = CffSubsetter.Read(CffSubsetter.Subset(cff, new ushort[] { 1 }));

            Assert.That(sub.CharStrings[1], Is.EqualTo(original.CharStrings[1]));
            Assert.That(sub.CharStrings[0], Is.EqualTo(original.CharStrings[0]), ".notdef is always kept");
            foreach (int g in new[] { 2, 3, 4 })
                Assert.That(sub.CharStrings[g], Is.EqualTo(Endchar), $"glyph {g} is not used");

            // local 0 is called by glyph 1 and calls global 1; nothing else is reached
            Assert.That(sub.LocalSubrs[0][0], Is.EqualTo(original.LocalSubrs[0][0]));
            Assert.That(sub.LocalSubrs[0][1], Is.EqualTo(Return));
            Assert.That(sub.LocalSubrs[0][2], Is.EqualTo(Return));
            Assert.That(sub.GlobalSubrs[1], Is.EqualTo(Body));
            Assert.That(sub.GlobalSubrs[0], Is.EqualTo(Return));
            Assert.That(sub.GlobalSubrs, Has.Length.EqualTo(2), "subroutine count (and so the bias) is unchanged");
        }

        [Test]
        public void Subset_SkipsHintmaskBytesWhenFindingSubroutineCalls()
        {
            var cff = BuildNameKeyedCff();
            var sub = CffSubsetter.Read(CffSubsetter.Subset(cff, new ushort[] { 4 }));

            // the mask byte 0x20 looks like a number; reading it as one would call the wrong subroutine
            Assert.That(sub.LocalSubrs[0][2], Is.EqualTo(Body));
            Assert.That(sub.LocalSubrs[0][0], Is.EqualTo(Return));
            Assert.That(sub.LocalSubrs[0][1], Is.EqualTo(Return));
        }

        [Test]
        public void Subset_FollowsGlobalCalls_AndKeepsEverythingWhenNoneIsNamed()
        {
            var cff = BuildNameKeyedCff();
            var sub = CffSubsetter.Read(CffSubsetter.Subset(cff, new ushort[] { 2 }));
            Assert.That(sub.GlobalSubrs[0], Is.EqualTo(Body));
            Assert.That(sub.GlobalSubrs[1], Is.EqualTo(Return));

            var all = CffSubsetter.Read(CffSubsetter.Subset(cff, null));
            var original = CffSubsetter.Read(cff);
            for (int g = 0; g < 5; g++) Assert.That(all.CharStrings[g], Is.EqualTo(original.CharStrings[g]));
            Assert.That(all.LocalSubrs[0], Is.EqualTo(original.LocalSubrs[0]));
            Assert.That(all.GlobalSubrs, Is.EqualTo(original.GlobalSubrs));
        }

        [Test]
        public void Subset_IsStableWhenRepeated()
        {
            var once = CffSubsetter.Subset(BuildNameKeyedCff(), new ushort[] { 1, 4 });
            var twice = CffSubsetter.Subset(once, new ushort[] { 1, 4 });
            Assert.That(twice, Is.EqualTo(once));
        }

        [Test]
        public void Subset_RejectsATableThatIsNotCff()
        {
            Assert.Throws<InvalidDataException>(() => CffSubsetter.Subset(new byte[] { 2, 0, 5, 0, 0, 0 }, null));
        }

        // ── the scanner ──────────────────────────────────────────────────────

        [Test]
        public void Scanner_SkipsAnOtfThatHasNoCffTable()
        {
            string dir = Path.Combine(Path.GetTempPath(), "cff-scan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            // 'OTTO' with no tables: neither glyf nor CFF, so it is not a font the embedder can use
            File.WriteAllBytes(Path.Combine(dir, "empty.otf"), new byte[] { 0x4F, 0x54, 0x54, 0x4F, 0, 0, 0, 0, 0, 0, 0, 0 });
            Assert.That(InstalledFontScanner.Scan(dir), Is.Empty);
        }

        // ── real fonts ───────────────────────────────────────────────────────

        private static readonly string[] NotoCjkRegular =
        {
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/noto-cjk/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/google-noto-cjk/NotoSansCJK-Regular.ttc",
            "/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc",
        };

        private static string RequireNotoCjk()
        {
            string? path = NotoCjkRegular.FirstOrDefault(File.Exists);
            Assume.That(path, Is.Not.Null, "Noto Sans CJK is not installed");
            return path!;
        }

        private const string CjkText = "こんにちは 你好世界 한국어 Hello";

        private static byte[] RenderWithNotoCjk(string path, string text = CjkText, string family = "Noto Sans CJK JP")
        {
            var reg = new FontRegistry().AddInstalledFonts(Path.GetDirectoryName(path)!);
            Assume.That(reg.Contains(family), Is.True, family + " is not in " + path);
            return PdfDocument.Create()
                .WithFontRegistry(reg)
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100, TextStyle.Default.WithFamily(family).WithSize(24)))
                .ToBytes();
        }

        [Test]
        public void Scanner_FindsNotoSansCjk()
        {
            string path = RequireNotoCjk();
            var faces = InstalledFontScanner.ReadFaces(path);
            Assert.That(faces.Select(f => f.Family), Does.Contain("Noto Sans CJK JP"));
            Assert.That(faces.Select(f => f.FaceIndex).Distinct().Count(), Is.EqualTo(faces.Count));
        }

        [Test]
        public void NotoSansCjk_SubsetGivesAnOpenTypeFontWithIdentityCharset()
        {
            string path = RequireNotoCjk();
            var ttf = new TrueTypeFont(File.ReadAllBytes(path), 0);
            Assert.That(ttf.IsCff, Is.True);

            ushort a = ttf.GetGlyphId('A'), ka = ttf.GetGlyphId('か'), han = ttf.GetGlyphId('世');
            Assert.That(new[] { a, ka, han }, Has.None.EqualTo((ushort)0));

            byte[] subset = ttf.Subset(new HashSet<ushort> { a, ka, han });
            Assert.That(Encoding.ASCII.GetString(subset, 0, 4), Is.EqualTo("OTTO"));

            var re = new TrueTypeFont(subset);
            Assert.That(re.IsCff, Is.True);
            Assert.That(re.NumGlyphs, Is.EqualTo(ttf.NumGlyphs), "glyph IDs are unchanged");
            Assert.That(re.GetGlyphId('か'), Is.EqualTo(ka));
            Assert.That(re.GetAdvanceWidth(han), Is.EqualTo(ttf.GetAdvanceWidth(han)));

            var cff = ExtractTable(subset, "CFF ");
            var contents = CffSubsetter.Read(cff);
            Assert.That(contents.IsCid, Is.True);
            Assert.That(contents.Cids!.Select((c, g) => c == g || g == 0).All(x => x), Is.True, "CID == glyph ID");

            var originalContents = CffSubsetter.Read(ExtractTable(ttf.Data, "CFF ", 0));
            Assert.That(contents.CharStrings[han], Is.EqualTo(originalContents.CharStrings[han]));
            Assert.That(contents.CharStrings.Count(c => c.Length > 1), Is.LessThanOrEqualTo(3 + 1));

            // what the PDF embeds is the bare table, identical to the one inside the OpenType wrapper
            byte[] bare = ttf.SubsetCff(new HashSet<ushort> { a, ka, han });
            Assert.That(bare, Is.EqualTo(cff));
            Assert.That(bare.Length, Is.LessThan(ttf.Data.Length / 20));
        }

        [Test]
        public void NotoSansCjk_PdfEmbedsASmallCidFontType0()
        {
            string path = RequireNotoCjk();
            byte[] pdf = RenderWithNotoCjk(path);

            string raw = Encoding.Latin1.GetString(pdf);
            Assert.That(raw, Does.Contain("/CIDFontType0"));
            Assert.That(raw, Does.Contain("/FontFile3"));
            Assert.That(raw, Does.Contain("/Subtype /CIDFontType0C"));
            Assert.That(raw, Does.Not.Contain("/CIDFontType2"));
            Assert.That(raw, Does.Not.Contain("/CIDToGIDMap"));

            // The whole file is 16-20 MB; the subset must be a small fraction of that.
            Assert.That(pdf.Length, Is.LessThan(300_000), "PDF size " + pdf.Length);
            var dump = Environment.GetEnvironmentVariable("CFF_TEST_DUMP");
            if (!string.IsNullOrEmpty(dump)) File.WriteAllBytes(dump, pdf);

            using var doc = PdfPig.Open(pdf);
            string text = string.Join(" ", doc.GetPage(1).GetWords().Select(w => w.Text));
            Assert.That(text, Is.EqualTo(CjkText));
        }

        [Test]
        public void NotoSansCjk_Pdf_IsAcceptedByPoppler()
        {
            string path = RequireNotoCjk();
            Assume.That(Which("pdffonts"), Is.Not.Null, "pdffonts is not installed");
            Assume.That(Which("pdftotext"), Is.Not.Null, "pdftotext is not installed");

            string file = Path.Combine(Path.GetTempPath(), "cff-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                File.WriteAllBytes(file, RenderWithNotoCjk(path));

                var fonts = Run("pdffonts", file);
                Assert.That(fonts.stdout, Does.Contain("CID Type 0C"), fonts.stdout + fonts.stderr);
                Assert.That(fonts.stdout, Does.Contain("yes"), "embedded: " + fonts.stdout);
                Assert.That(fonts.stderr, Is.Empty, fonts.stderr);

                var textOut = Run("pdftotext", file + " -");
                Assert.That(textOut.stdout, Does.Contain("こんにちは 你好世界 한국어 Hello"), textOut.stderr);
                Assert.That(textOut.stderr, Is.Empty, textOut.stderr);

                if (Which("qpdf") != null)
                {
                    var q = Run("qpdf", "--check " + file);
                    Assert.That(q.stdout + q.stderr, Does.Contain("No syntax or stream encoding errors"));
                }
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void NotoSansCjk_EncryptedPdfStillCarriesTheFont()
        {
            // The font stream goes through the encryptor, which has its own header path for /Subtype /CIDFontType0C.
            string path = RequireNotoCjk();
            var reg = new FontRegistry().AddInstalledFonts(Path.GetDirectoryName(path)!);
            Assume.That(reg.Contains("Noto Sans CJK JP"), Is.True);
            byte[] pdf = PdfDocument.Create()
                .WithFontRegistry(reg)
                .WithSecurity(PdfSecurity.Protect("user", ownerPassword: "owner"))
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("你好世界", 72, 100, TextStyle.Default.WithFamily("Noto Sans CJK JP").WithSize(24)))
                .ToBytes();
            Assert.That(Encoding.Latin1.GetString(pdf), Does.Contain("/Subtype /CIDFontType0C"));
            using var doc = PdfPig.Open(pdf, new UglyToad.PdfPig.ParsingOptions { Passwords = new List<string> { "user" } });
            Assert.That(string.Join("", doc.GetPage(1).GetWords().Select(w => w.Text)), Is.EqualTo("你好世界"));
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static byte[] ExtractTable(byte[] font, string tag, int fontOffset = 0)
        {
            if (Encoding.ASCII.GetString(font, 0, 4) == "ttcf")
                fontOffset = (font[12] << 24) | (font[13] << 16) | (font[14] << 8) | font[15];
            int n = (font[fontOffset + 4] << 8) | font[fontOffset + 5];
            for (int i = 0; i < n; i++)
            {
                int e = fontOffset + 12 + i * 16;
                if (Encoding.ASCII.GetString(font, e, 4) != tag) continue;
                int off = (font[e + 8] << 24) | (font[e + 9] << 16) | (font[e + 10] << 8) | font[e + 11];
                int len = (font[e + 12] << 24) | (font[e + 13] << 16) | (font[e + 14] << 8) | font[e + 15];
                var t = new byte[len];
                Buffer.BlockCopy(font, off, t, 0, len);
                return t;
            }
            throw new InvalidDataException(tag + " not found");
        }

        private static string? Which(string tool) =>
            (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
                .Select(d => Path.Combine(d, tool)).FirstOrDefault(File.Exists);

        private static (string stdout, string stderr) Run(string tool, string args)
        {
            var psi = new ProcessStartInfo(Which(tool)!, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi)!;
            string err = p.StandardError.ReadToEnd();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return (output, err);
        }
    }
}
