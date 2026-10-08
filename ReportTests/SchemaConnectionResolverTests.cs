using System;
using System.Collections.Generic;
using Majorsilence.Reporting.WebDesigner;
using NUnit.Framework;

namespace ReportTests
{
    [TestFixture]
    public class SchemaConnectionResolverTests
    {
        private static Dictionary<string, RdlSchemaConnection> Configured() =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Sales"] = new RdlSchemaConnection { DataProvider = "Json", ConnectionString = "file=/srv/sales.json" },
            };

        [Test]
        public void NamedConnection_UsesServerValues_NotTheRequest()
        {
            var r = SchemaConnectionResolver.Resolve(Configured(), false, "sales", "odbc", "evil", out var error);
            Assert.That(error, Is.Null);
            Assert.That(r.DataProvider, Is.EqualTo("Json"));
            Assert.That(r.ConnectionString, Is.EqualTo("file=/srv/sales.json"));
        }

        [Test]
        public void UnknownName_WithoutClientStrings_IsRejected()
        {
            var r = SchemaConnectionResolver.Resolve(Configured(), false, "other", "odbc", "dsn=x", out var error);
            Assert.That(r, Is.Null);
            Assert.That(error, Does.Contain("SchemaConnections"));
        }

        [Test]
        public void ClientStrings_AreRejectedByDefault()
        {
            var r = SchemaConnectionResolver.Resolve(Configured(), false, null, "odbc", "dsn=x", out _);
            Assert.That(r, Is.Null);
        }

        [Test]
        public void ClientStrings_AreAcceptedWhenEnabled()
        {
            var r = SchemaConnectionResolver.Resolve(Configured(), true, null, " odbc ", " dsn=x ", out var error);
            Assert.That(error, Is.Null);
            Assert.That(r.DataProvider, Is.EqualTo("odbc"));
            Assert.That(r.ConnectionString, Is.EqualTo("dsn=x"));
        }

        [TestCase("filedirectory")]
        [TestCase("XML")]
        [TestCase("webservice")]
        [TestCase("weblog")]
        [TestCase("text")]
        [TestCase("Json")]
        [TestCase("itunes")]
        public void FileAndWebProviders_AreRejectedFromTheClientEvenWhenEnabled(string provider)
        {
            var r = SchemaConnectionResolver.Resolve(new Dictionary<string, RdlSchemaConnection>(), true, null, provider, "x", out var error);
            Assert.That(r, Is.Null);
            Assert.That(error, Does.Contain("configured on the server"));
        }

        [TestCase(null, "x")]
        [TestCase("odbc", "")]
        public void MissingValues_AreRejected(string provider, string cs)
        {
            Assert.That(SchemaConnectionResolver.Resolve(Configured(), true, null, provider, cs, out _), Is.Null);
        }
    }
}
