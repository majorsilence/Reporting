#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A report's &lt;Code&gt; block that was compiled at build time (RdlCodeGen) is registered by the
    /// hash of its text and then wins over runtime VB compilation, which Native AOT cannot do.
    /// </summary>
    [TestFixture]
    public class PrecompiledCodeTests
    {
        const string CodeText = "Function Twice(n As Integer) As Integer\r\n    Return n * 2\r\nEnd Function";

        static string Rdl(string code) => @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>5in</Width>
  <Body><Height>1in</Height>
    <ReportItems>
      <Textbox Name=""T""><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>.5in</Height>
        <Value>=""RESULT["" &amp; Code.Twice(21) &amp; ""]""</Value></Textbox>
    </ReportItems>
  </Body>
  <Code>" + code + @"</Code>
</Report>";

        static async Task<string> RenderAsync(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            Report report = await new RDLParser(rdl).Parse();
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public void Hash_IgnoresLineEndingsAndSurroundingWhitespace()
        {
            Assert.That(RdlCodeHash.Compute("a\r\nb"), Is.EqualTo(RdlCodeHash.Compute("\n  a\nb\n\n")));
            Assert.That(RdlCodeHash.Compute("a"), Is.Not.EqualTo(RdlCodeHash.Compute("b")));
        }

        [Test]
        public async Task PrecompiledBlock_IsUsedForTheMatchingCode()
        {
            RdlEngineConfig.RegisterPrecompiledCode(RdlCodeHash.Compute(CodeText),
                _ => new RdlCodeFunctions().Add("Twice", a => (int)a[0]! * 2));

            string html = await RenderAsync(Rdl(CodeText));

            Assert.That(html, Does.Contain("RESULT[42]"));
        }

        [Test]
        public async Task CodeWithoutAPrecompiledBlock_IsNotServedByAnotherBlock()
        {
            RdlEngineConfig.RegisterPrecompiledCode(RdlCodeHash.Compute(CodeText),
                _ => new RdlCodeFunctions().Add("Twice", a => (int)a[0]! * 2));

            // Different text -> different hash. Runtime VB compilation is unavailable off .NET Framework, so
            // the block yields a report error instead of silently running someone else's code.
            string other = CodeText.Replace("n * 2", "n * 3");
            string html = await RenderAsync(Rdl(other));

            Assert.That(html, Does.Not.Contain("RESULT[42]"));
        }
    }
}
#endif
