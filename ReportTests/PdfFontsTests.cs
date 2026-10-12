#if NET8_0_OR_GREATER
using Majorsilence.Pdf;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;

namespace ReportTests
{
    /// <summary>
    /// The public API for adding fonts to PDF export (#358). The registrations are process-wide, so
    /// these tests do not run in parallel with other renders and always reset what they registered.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class PdfFontsTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _outputFolder = GeneralUtils.OutputTestsFolder();
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
            PdfFonts.Reset();
            _tempDir = Path.Combine(Path.GetTempPath(), "pdffonts-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            PdfFonts.Reset();
            Environment.SetEnvironmentVariable(PdfFonts.FontDirectoriesVariable, null);
            try { Directory.Delete(_tempDir, true); } catch (IOException) { }
        }

        private static string Bundled(string name)
        {
            string dir = Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();
            string path = Path.Combine(dir, name);
            Assume.That(File.Exists(path), Is.True, name + " is not bundled");
            return path;
        }

        // The font the PDF draws the text of InstalledFontTest.rdl's Arial textbox in.
        private async Task<string> ArialTextFont()
        {
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, "InstalledFontTest.rdl"));
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData(null);
            string output = Path.Combine(_outputFolder.LocalPath, "PdfFontsTest.pdf");
            using (var sg = new OneFileStreamGen(output, true))
                await report.RunRender(sg, OutputPresentationType.PDF);

            using var pdf = PdfPigDocument.Open(output);
            var letters = pdf.GetPages().Single().Letters.ToList();
            string drawn = string.Concat(letters.Select(l => l.Value));
            int at = drawn.IndexOf("ARIALTEXT", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), $"drawn: '{drawn}'");
            return letters[at].FontName;
        }

        // ── end to end ───────────────────────────────────────────────────────

        [Test]
        public async Task RegisterFont_ByPath_IsUsedForTheFamily_EvenAfterAnEarlierRender()
        {
            string before = await ArialTextFont();
            Assert.That(before, Does.Not.Contain("LiberationMono"));

            // a registration made after the shared registry was built still takes effect
            PdfFonts.RegisterFont("Arial", regular: Bundled("LiberationMono-Regular.ttf"));

            Assert.That(await ArialTextFont(), Does.Contain("LiberationMono"));

            PdfFonts.Reset();
            Assert.That(await ArialTextFont(), Is.EqualTo(before), "reset restores the built-in fonts");
        }

        [Test]
        public async Task RegisterFont_FromBytes_IsUsedForTheFamily()
        {
            PdfFonts.RegisterFont("Arial", File.ReadAllBytes(Bundled("LiberationMono-Regular.ttf")));

            Assert.That(await ArialTextFont(), Does.Contain("LiberationMono"));
        }

        [Test]
        public void RegisterFontDirectory_RegistersFontsUnderTheFamilyTheyDeclare_NotTheirFileName()
        {
            string mono = Bundled("LiberationMono-Regular.ttf");
            string copy = Path.Combine(_tempDir, "anything.ttf");
            File.Copy(mono, copy);
            string declared = MonoFamily;

            PdfFonts.RegisterFontDirectory(_tempDir);
            var reg = new FontRegistry();
            PdfFonts.ApplyTo(reg);
            Assert.That(reg.Contains(declared), Is.True);
            Assert.That(reg.Resolve(declared, false, false)?.Path, Is.EqualTo(copy));
        }

        // ── registry level ───────────────────────────────────────────────────

        // The family Liberation Mono declares for itself, which is what a folder scan registers it as.
        private const string MonoFamily = "Liberation Mono", SerifFamily = "Liberation Serif";

        [Test]
        public void ApplyTo_CallerFamilyReplacesAScannedOneOfTheSameName()
        {
            string a = Bundled("LiberationMono-Regular.ttf");
            string b = Bundled("LiberationSerif-Regular.ttf");
            var reg = new FontRegistry().AddFamily("Impact", regular: a);

            PdfFonts.RegisterFont("Impact", regular: b);
            PdfFonts.ApplyTo(reg);

            Assert.That(reg.Resolve("Impact", false, false)?.Path, Is.EqualTo(b));
        }

        [Test]
        public void ApplyTo_FamilyGivenToRegisterFont_BeatsOneFromAFolder()
        {
            string mono = Bundled("LiberationMono-Regular.ttf");
            string serif = Bundled("LiberationSerif-Regular.ttf");
            string inFolder = Path.Combine(_tempDir, "a.ttf");
            File.Copy(mono, inFolder);
            string family = MonoFamily;

            PdfFonts.RegisterFontDirectory(_tempDir);
            PdfFonts.RegisterFont(family, regular: serif);
            var reg = new FontRegistry();
            PdfFonts.ApplyTo(reg);

            Assert.That(reg.Resolve(family, false, false)?.Path, Is.EqualTo(serif));
        }

        [Test]
        public void ApplyTo_OfTwoFolders_TheOneRegisteredFirstWins()
        {
            string first = Path.Combine(_tempDir, "first"), second = Path.Combine(_tempDir, "second");
            Directory.CreateDirectory(first); Directory.CreateDirectory(second);
            File.Copy(Bundled("LiberationMono-Regular.ttf"), Path.Combine(first, "m.ttf"));
            File.Copy(Bundled("LiberationMono-Regular.ttf"), Path.Combine(second, "m.ttf"));
            string family = MonoFamily;

            PdfFonts.RegisterFontDirectory(first);
            PdfFonts.RegisterFontDirectory(second);
            var reg = new FontRegistry();
            PdfFonts.ApplyTo(reg);

            Assert.That(reg.Resolve(family, false, false)?.Path, Is.EqualTo(Path.Combine(first, "m.ttf")));
        }

        [Test]
        public void RegisterFontDirectory_SubfoldersOnlyWhenAsked()
        {
            string sub = Path.Combine(_tempDir, "deep");
            Directory.CreateDirectory(sub);
            File.Copy(Bundled("LiberationMono-Regular.ttf"), Path.Combine(sub, "m.ttf"));
            string family = MonoFamily;

            PdfFonts.RegisterFontDirectory(_tempDir, includeSubfolders: false);
            var flat = new FontRegistry();
            PdfFonts.ApplyTo(flat);
            Assert.That(flat.Contains(family), Is.False);

            PdfFonts.RegisterFontDirectory(_tempDir, includeSubfolders: true);
            var deep = new FontRegistry();
            PdfFonts.ApplyTo(deep);
            Assert.That(deep.Contains(family), Is.True);
        }

        [Test]
        public void RegisterFontDirectory_MissingFolderAddsNothing()
        {
            PdfFonts.RegisterFontDirectory(Path.Combine(_tempDir, "nope"));
            var reg = new FontRegistry();
            Assert.DoesNotThrow(() => PdfFonts.ApplyTo(reg));
        }

        [Test]
        public void EnvironmentVariable_NamesFontFolders()
        {
            string dirA = Path.Combine(_tempDir, "a"), dirB = Path.Combine(_tempDir, "b");
            Directory.CreateDirectory(dirA); Directory.CreateDirectory(dirB);
            File.Copy(Bundled("LiberationMono-Regular.ttf"), Path.Combine(dirA, "m.ttf"));
            File.Copy(Bundled("LiberationSerif-Regular.ttf"), Path.Combine(dirB, "s.ttf"));
            string mono = MonoFamily, serif = SerifFamily;

            Environment.SetEnvironmentVariable(PdfFonts.FontDirectoriesVariable,
                string.Join(Path.PathSeparator, dirA, dirB));
            var reg = new FontRegistry();
            PdfFonts.ApplyTo(reg);

            Assert.That(reg.Contains(mono), Is.True);
            Assert.That(reg.Contains(serif), Is.True);
        }

        [Test]
        public void EnvironmentVariable_IsBeatenByARegisteredFolder()
        {
            string env = Path.Combine(_tempDir, "env"), app = Path.Combine(_tempDir, "app");
            Directory.CreateDirectory(env); Directory.CreateDirectory(app);
            File.Copy(Bundled("LiberationMono-Regular.ttf"), Path.Combine(env, "m.ttf"));
            File.Copy(Bundled("LiberationMono-Regular.ttf"), Path.Combine(app, "m.ttf"));
            string family = MonoFamily;

            Environment.SetEnvironmentVariable(PdfFonts.FontDirectoriesVariable, env);
            PdfFonts.RegisterFontDirectory(app);
            var reg = new FontRegistry();
            PdfFonts.ApplyTo(reg);

            Assert.That(reg.Resolve(family, false, false)?.Path, Is.EqualTo(Path.Combine(app, "m.ttf")));
        }

        [Test]
        public void AddFallbackFamily_ComesBeforeTheBuiltInChain_InTheOrderAdded()
        {
            string mono = Bundled("LiberationMono-Regular.ttf");
            string serif = Bundled("LiberationSerif-Regular.ttf");
            string sans = Bundled("LiberationSans-Regular.ttf");

            PdfFonts.RegisterFont("MyCjk", regular: mono);
            PdfFonts.RegisterFont("MySymbols", regular: serif);
            PdfFonts.AddFallbackFamily("MyCjk");
            PdfFonts.AddFallbackFamily("MySymbols");
            PdfFonts.AddFallbackFamily("mycjk"); // already there

            var reg = new FontRegistry().AddFamily("BuiltIn", regular: sans);
            PdfFonts.ApplyTo(reg);
            reg.AddFallback("BuiltIn"); // what the renderer adds afterwards

            var paths = reg.GetFallbackSources(false, false).Select(s => s.Path).ToList();
            Assert.That(paths, Is.EqualTo(new[] { mono, serif, sans }));
        }

        [Test]
        public void RegisterFont_RegisteringANameAgainReplacesIt()
        {
            string mono = Bundled("LiberationMono-Regular.ttf");
            string serif = Bundled("LiberationSerif-Regular.ttf");
            PdfFonts.RegisterFont("Barcode", regular: mono);
            PdfFonts.RegisterFont("barcode", regular: serif);

            var reg = new FontRegistry();
            PdfFonts.ApplyTo(reg);
            Assert.That(reg.Resolve("Barcode", false, false)?.Path, Is.EqualTo(serif));
        }

        [Test]
        public void RegisterFont_ValidatesItsArguments()
        {
            Assert.Throws<ArgumentNullException>(() => PdfFonts.RegisterFont("", regular: "a.ttf"));
            Assert.Throws<ArgumentException>(() => PdfFonts.RegisterFont("X"));
            Assert.Throws<ArgumentNullException>(() => PdfFonts.RegisterFontDirectory(""));
            Assert.Throws<ArgumentNullException>(() => PdfFonts.AddFallbackFamily(""));
        }
    }
}
#endif
