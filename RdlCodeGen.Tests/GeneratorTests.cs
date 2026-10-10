using Majorsilence.Reporting.RdlCodeGen;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace RdlCodeGen.Tests;

public class GeneratorTests
{
    string _dir = "";

    [SetUp] public void SetUp() { _dir = Directory.CreateTempSubdirectory("rdlcodegen").FullName; }
    [TearDown] public void TearDown() { try { Directory.Delete(_dir, true); } catch { } }

    string Report(string name, string? code, string ns = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition")
    {
        string path = Path.Combine(_dir, name);
        string codeEl = code == null ? "" : "<Code>" + System.Security.SecurityElement.Escape(code) + "</Code>";
        File.WriteAllText(path, $"<?xml version=\"1.0\"?><Report xmlns=\"{ns}\"><Width>1in</Width>{codeEl}</Report>");
        return path;
    }

    const string Grade = "Function Grade(score As Double) As String\r\n  If score >= 50 Then Return \"P\"\r\n  Return \"F\"\r\nEnd Function";

    [Test]
    public void ReportsWithoutCode_ProduceNoBlocks()
    {
        var gen = new Generator();
        Assert.That(gen.Collect(new[] { Report("a.rdl", null) }), Is.Empty);
        Assert.That(gen.HasErrors, Is.False);
    }

    [Test]
    public void IdenticalCodeInTwoReports_IsOneBlockWithBothReports()
    {
        var gen = new Generator();
        var blocks = gen.Collect(new[] { Report("a.rdl", Grade), Report("b.rdl", Grade.Replace("\r\n", "\n")) });
        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0].Reports, Has.Count.EqualTo(2));
        Assert.That(blocks[0].Hash, Is.EqualTo(RdlCodeHash.Compute(Grade)));
    }

    [Test]
    public void Namespace2008_IsRead()
    {
        var gen = new Generator();
        var blocks = gen.Collect(new[] { Report("a.rdl", Grade, "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition") });
        Assert.That(blocks, Has.Count.EqualTo(1));
    }

    [Test]
    public void Generated_RegistersTheBlockAndCastsArguments()
    {
        var gen = new Generator();
        string vb = gen.Generate(gen.Collect(new[] { Report("a.rdl", Grade) }));
        string hash = RdlCodeHash.Compute(Grade);
        Assert.That(gen.HasErrors, Is.False);
        Assert.That(vb, Does.Contain($"RegisterPrecompiledCode(\"{hash}\""));
        Assert.That(vb, Does.Contain($"Class MyClass_{hash}"));
        Assert.That(vb, Does.Contain("f.Add(\"Grade\""));
        Assert.That(vb, Does.Contain("inst.Grade(CType(a(0), Double))"));
    }

    [Test]
    public void ByRefMethod_IsSkippedWithAWarning()
    {
        var gen = new Generator();
        string vb = gen.Generate(gen.Collect(new[] { Report("a.rdl", "Function F(ByRef x As Integer) As Integer\r\n Return x\r\nEnd Function\r\nFunction G(y As Integer) As Integer\r\n Return y\r\nEnd Function") }));
        Assert.That(gen.HasErrors, Is.False);
        Assert.That(gen.Messages, Has.Some.Contain("RDLCODE003"));
        Assert.That(vb, Does.Not.Contain("f.Add(\"F\""));
        Assert.That(vb, Does.Contain("f.Add(\"G\""));
    }

    [Test]
    public void PrivateMethods_AreNotExposed()
    {
        var gen = new Generator();
        string vb = gen.Generate(gen.Collect(new[] { Report("a.rdl", "Private Function Hidden() As Integer\r\n Return 1\r\nEnd Function\r\nFunction Shown() As Integer\r\n Return 2\r\nEnd Function") }));
        Assert.That(vb, Does.Not.Contain("f.Add(\"Hidden\""));
        Assert.That(vb, Does.Contain("f.Add(\"Shown\""));
    }

    [Test]
    public void SubroutineReturnsNothing()
    {
        var gen = new Generator();
        string vb = gen.Generate(gen.Collect(new[] { Report("a.rdl", "Sub Touch(s As String)\r\nEnd Sub") }));
        Assert.That(vb, Does.Contain("inst.Touch(CType(a(0), String))"));
        Assert.That(vb, Does.Contain("Return Nothing"));
    }

    [Test]
    public void SyntaxError_IsReportedAsAnError()
    {
        var gen = new Generator();
        gen.Generate(gen.Collect(new[] { Report("a.rdl", "Function Broken( As\r\nEnd Function") }));
        Assert.That(gen.HasErrors, Is.True);
        Assert.That(gen.Messages, Has.Some.Contain("RDLCODE002"));
    }

    [Test]
    public void UnreadableReport_IsReportedAsAnError()
    {
        var gen = new Generator();
        string bad = Path.Combine(_dir, "bad.rdl");
        File.WriteAllText(bad, "not xml");
        gen.Collect(new[] { bad });
        Assert.That(gen.HasErrors, Is.True);
    }

    [Test]
    public void Output_IsDeterministic()
    {
        var gen = new Generator();
        var reports = new[] { Report("a.rdl", Grade), Report("b.rdl", "Function Other() As Integer\r\n Return 1\r\nEnd Function") };
        string one = gen.Generate(gen.Collect(reports));
        string two = new Generator().Generate(new Generator().Collect(reports.Reverse()));
        Assert.That(two, Is.EqualTo(one)); // blocks are ordered by hash, not by input order
        Assert.That(one.IndexOf("RegisterPrecompiledCode"), Is.GreaterThan(0));
    }
}
