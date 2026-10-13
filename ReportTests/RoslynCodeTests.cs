#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RoslynCode;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A report's &lt;Code&gt; element compiled at runtime on .NET 8 and later, through a
    /// registered compiler (<see cref="RdlEngineConfig.RegisterCodeCompiler"/>). The engine's
    /// own VBCodeProvider path only exists on .NET Framework, so without one the element
    /// logs an error and every Code.X call is empty.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class RoslynCodeTests
    {
        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            RoslynCodeCompiler.Register();
        }

        [TearDown]
        public void TearDown() => RdlEngineConfig.RegisterCodeCompiler(null);

        static string Rdl(string code, string value) => @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>5in</Width>
  <Body><Height>1in</Height>
    <ReportItems>
      <Textbox Name=""T""><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>.5in</Height>
        <Value>" + value + @"</Value></Textbox>
    </ReportItems>
  </Body>
  <Code>" + code + @"</Code>
</Report>";

        static async Task<(string Html, Report Report)> RenderAsync(string rdl)
        {
            Report report = await new RDLParser(rdl).Parse();
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return (ms.GetText(), report);
        }

        // The reproduction from the issue: a loop, a local, a function.
        const string Doubled = @"
Public Function Doubled(ByVal n As Integer) As Integer
  Dim acc As Integer = 0
  For i As Integer = 1 To 2
    acc = acc + n
  Next
  Return acc
End Function";

        [Test]
        public async Task ALoopInACodeBlock_Runs()
        {
            var (html, _) = await RenderAsync(Rdl(Doubled, @"=""RESULT["" &amp; Code.Doubled(21) &amp; ""]"""));
            Assert.That(html, Does.Contain("RESULT[42]"));
        }

        // Shared state survives from one call to the next. Idempotent on purpose: the engine
        // may evaluate a value more than once while it lays a page out, so a test of a running
        // count would be a test of how often it does that.
        const string Store = @"
Private Shared storedValue As String = ""unset""
Public Function Keep(ByVal v As String) As String
  storedValue = v
  Return ""kept""
End Function
Public Function Kept() As String
  Return storedValue
End Function";

        [Test]
        public async Task SharedStateCarriesAcrossCalls()
        {
            var (html, _) = await RenderAsync(Rdl(Store, @"=Code.Keep(""ZZ-STORED"") &amp; ""/"" &amp; Code.Kept()"));
            Assert.That(html, Does.Contain("kept/ZZ-STORED"), "the second call sees what the first stored");
        }

        [Test]
        public async Task ACompileError_IsReportedWithItsLineInTheCodeElement()
        {
            const string broken = "Public Function Bad() As Integer\r\n  Return undefinedName\r\nEnd Function";
            var report = await new RDLParser(Rdl(broken, "=Code.Bad()")).Parse();

            var errors = report.ErrorItems?.Cast<string>().ToList() ?? new();
            Assert.That(errors, Has.Some.Contains("Code element has"), string.Join(" | ", errors));
            Assert.That(errors, Has.Some.Contains("Line 2:"), "the undefined name is on the block's second line");
        }
    }
}
#endif
