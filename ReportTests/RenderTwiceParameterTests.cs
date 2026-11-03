using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A report rendered more than once after one RunGetData keeps its parameter values for every
    /// render, as RdlCmd does when asked for several output types. Each render reset the report's
    /// run cache, keeping only the fetched rows, so the second render had no parameter values and
    /// a subreport handed one got nothing (#537).
    /// </summary>
    [TestFixture]
    public class RenderTwiceParameterTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-render-twice-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "Child.rdl"), ChildRdl);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private const string ParentRdl = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <ReportParameters>
                <ReportParameter Name="Greeting"><DataType>String</DataType><Prompt>Greeting</Prompt></ReportParameter>
              </ReportParameters>
              <Width>6in</Width>
              <Body>
                <Height>2in</Height>
                <ReportItems>
                  <Textbox Name="ParentValue">
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>="Parent sees: " &amp; Parameters!Greeting.Value</Value>
                  </Textbox>
                  <Subreport Name="Child">
                    <ReportName>Child</ReportName>
                    <Parameters>
                      <Parameter Name="Greeting"><Value>=Parameters!Greeting.Value</Value></Parameter>
                      <Parameter Name="Constant"><Value>constant-ok</Value></Parameter>
                    </Parameters>
                    <Top>0.5in</Top><Left>0in</Left><Width>6in</Width><Height>1in</Height>
                  </Subreport>
                </ReportItems>
              </Body>
            </Report>
            """;

        private const string ChildRdl = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <ReportParameters>
                <ReportParameter Name="Greeting"><DataType>String</DataType><Nullable>true</Nullable><Prompt>Greeting</Prompt></ReportParameter>
                <ReportParameter Name="Constant"><DataType>String</DataType><Nullable>true</Nullable><Prompt>Constant</Prompt></ReportParameter>
              </ReportParameters>
              <Width>6in</Width>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <Textbox Name="ChildGreeting">
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>="Child Greeting: [" &amp; Parameters!Greeting.Value &amp; "]"</Value>
                  </Textbox>
                  <Textbox Name="ChildConstant">
                    <Top>0.3in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>="Child Constant: [" &amp; Parameters!Constant.Value &amp; "]"</Value>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

        private static async Task<string> RenderHtmlAsync(Report report)
        {
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task ASecondRender_KeepsTheParameterValues()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(ParentRdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(new Hashtable { ["Greeting"] = "hello" });

            string first = await RenderHtmlAsync(report);
            string second = await RenderHtmlAsync(report);

            foreach (string html in new[] { first, second })
            {
                Assert.That(html, Does.Contain("Parent sees: hello"));
                Assert.That(html, Does.Contain("Child Greeting: [hello]"));
                Assert.That(html, Does.Contain("Child Constant: [constant-ok]"));
            }
        }
    }
}
