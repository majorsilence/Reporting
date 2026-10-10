using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Class elements may carry literal Parameters that are handed to a matching
    /// constructor, or to SetParameters after default construction (issue #133).
    /// </summary>
    [TestFixture]
    public class ClassInstanceParametersTests
    {
        public class CtorGreeter
        {
            private readonly string _greeting;
            private readonly int _times;
            public CtorGreeter(string greeting, int times) { _greeting = greeting; _times = times; }
            public string Greet() => string.Concat(System.Linq.Enumerable.Repeat(_greeting, _times));
        }

        public class SetterGreeter
        {
            private string _greeting = "unset";
            public void SetParameters(string greeting) { _greeting = greeting; }
            public string Greet() => _greeting;
        }

        private static string Rdl(string cls, string parameters) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>5in</Width>
  <CodeModules><CodeModule>{typeof(ClassInstanceParametersTests).Assembly.Location}</CodeModule></CodeModules>
  <Classes><Class><ClassName>{cls}</ClassName><InstanceName>G</InstanceName>{parameters}</Class></Classes>
  <Body><Height>1in</Height><ReportItems>
    <Textbox Name=""T""><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>.25in</Height><Value>=G.Greet()</Value></Textbox>
  </ReportItems></Body>
</Report>";

        private static async Task<string> RenderAsync(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var parser = new RDLParser(rdl) { Folder = Directory.GetCurrentDirectory() };
            Report report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8));
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task Parameters_ArePassedToConstructor()
        {
            string html = await RenderAsync(Rdl(typeof(CtorGreeter).FullName,
                "<Parameters><Parameter><Value>Hi</Value></Parameter><Parameter><Value>3</Value></Parameter></Parameters>"));
            Assert.That(html, Does.Contain("HiHiHi"));
        }

        [Test]
        public async Task Parameters_AreSentToSetParameters()
        {
            string html = await RenderAsync(Rdl(typeof(SetterGreeter).FullName,
                "<Parameters><Parameter><Value>Hello133</Value></Parameter></Parameters>"));
            Assert.That(html, Does.Contain("Hello133"));
        }
    }
}
