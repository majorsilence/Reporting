#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// Issue #101: Using query parameters with an OLEDB (or ODBC) data source
    /// failed with "OleDbCommand.Prepare method requires all parameters to have an
    /// explicitly set type". OLE DB/ODBC providers use positional '?' parameters and
    /// cannot bind the named '@parameter' values the engine creates, so the engine
    /// must substitute parameter values as literals into the SQL text instead
    /// (ReplaceParameters). ODBC was already configured this way; OLEDB was not.
    /// </summary>
    [TestFixture]
    public class OleDbParameterReplacementTest
    {
        [SetUp]
        public void SetUp()
        {
            // No directory args -> engine uses its built-in default configuration.
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public void OledbUsesParameterReplacement()
        {
            Assert.That(RdlEngineConfig.DoParameterReplacement("OLEDB", null), Is.True,
                "OLEDB must substitute query parameters as literals; it cannot bind named parameters.");
        }

        [Test]
        public void OdbcUsesParameterReplacement()
        {
            // Guards against regressing the ODBC behaviour that OLEDB now mirrors.
            Assert.That(RdlEngineConfig.DoParameterReplacement("ODBC", null), Is.True,
                "ODBC must substitute query parameters as literals.");
        }
    }
}
#endif
