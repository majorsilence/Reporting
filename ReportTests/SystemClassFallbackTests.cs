using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A system class the parser knows -- Math, String, Convert -- resolves its overloads by
    /// the argument types the parser inferred, and an argument it could not type is Object,
    /// which matches nothing. Convert falls back to VBFunctions for exactly that reason,
    /// because VBFunctions mirrors Convert's methods under the same names and the same
    /// meaning.
    /// <para>
    /// The other classes must not. VBFunctions also carries VB's own runtime library, where a
    /// name can be shared with a System method that takes its arguments differently: VB's
    /// Join(values, delimiter) against String.Join(separator, values). Falling back there
    /// binds the delimiter as the list and the list as the delimiter, so the report renders
    /// the delimiter on its own -- a wrong answer where there used to be a parse error.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemClassFallbackTests
    {
        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static string ReportWithExpression(string expression)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>6in</Width>
  <ReportParameters>
    <ReportParameter Name=""M"">
      <DataType>String</DataType>
      <MultiValue>true</MultiValue>
      <DefaultValue><Values><Value>a</Value><Value>b</Value></Values></DefaultValue>
      <Prompt>M</Prompt>
    </ReportParameter>
  </ReportParameters>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""T"">
        <Value>={expression}</Value>
        <Top>0.1in</Top><Left>0.1in</Left><Height>0.3in</Height><Width>5in</Width>
      </Textbox>
    </ReportItems>
  </Body>
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>
</Report>";
        }

        private static async Task<(int Severity, string Html)> Render(string expression)
        {
            var parser = new RDLParser(ReportWithExpression(expression))
            {
                SkipDatabaseSchemaValidation = true
            };
            using var report = await parser.Parse();
            Assert.That(report, Is.Not.Null, "report failed to parse");

            await report.RunGetData(null);

            using var memory = new MemoryStreamGen();
            await report.RunRender(memory, OutputPresentationType.HTML);
            return (report.ErrorMaxSeverity, memory.GetText());
        }

        /// <summary>
        /// The case from the review. Whatever String.Join does here, it must not come out as
        /// the separator by itself -- that is VB's Join reading the arguments backwards.
        /// </summary>
        [Test]
        public async Task StringJoin_DoesNotSilentlyBindToVbJoin()
        {
            var (severity, html) = await Render(@"String.Join("", "", Parameters!M.Value)");

            Assert.That(html, Does.Not.Contain(">, <"),
                "String.Join rendered its separator alone, so it bound to VB's Join with the "
                + "arguments in the other order. An unresolvable overload has to stay an error.");

            // Either it resolves as String.Join and lists the values, or it does not resolve
            // at all and the report says so. Both are honest; silence is not.
            if (severity < 8)
                Assert.That(html, Does.Contain("a, b"),
                    "if String.Join binds at all it must produce the joined values");
        }

        /// <summary>
        /// VB's own Join, in VB's argument order, keeps working -- this is the supported way
        /// to flatten a multi-value parameter, and narrowing the fallback must not touch it.
        /// </summary>
        [Test]
        public async Task VbJoin_StillJoinsAMultiValueParameter()
        {
            var (severity, html) = await Render(@"Join(Parameters!M.Value, "", "")");

            Assert.That(severity, Is.LessThan(8), "Join should parse and evaluate");
            Assert.That(html, Does.Contain("a, b"));
        }

        /// <summary>
        /// The fallback that is kept: Convert's overloads are typed, the argument here is not,
        /// and VBFunctions' mirror means the same thing. This is what the fallback was for.
        /// </summary>
        [Test]
        public async Task ConvertStillFallsBackForAnUntypedArgument()
        {
            var (severity, html) = await Render(@"Convert.ToString(Parameters!M.Value(0))");

            Assert.That(severity, Is.LessThan(8), "Convert.ToString should still resolve");
            Assert.That(html, Does.Contain("a"));
        }
    }
}
