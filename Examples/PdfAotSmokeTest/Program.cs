// PdfAotSmokeTest — exercises Majorsilence.Pdf + Majorsilence.Pdf.Security under Native AOT.
//
// Publish and run as a self-contained AOT binary to verify the library actually works when
// compiled ahead-of-time (not just that it compiles without trim/AOT analyzer warnings):
//
//   dotnet publish -c Release -r linux-x64 -p:PublishAot=true --self-contained true
//   ./bin/Release/net10.0/linux-x64/publish/PdfAotSmokeTest
//
// Exit code 0 + "AOT-SMOKE-TEST-OK" on stdout means every code path below ran successfully
// under AOT and produced a structurally valid PDF each time.
//
// Exercises: text, tables, PdfLayout (Row/Column/flowing text), AES password protection,
// PKCS#7 digital signatures, and multi-document merge.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using Majorsilence.Pdf.Security;

bool ok = true;
void Check(string name, bool cond)
{
    Console.WriteLine((cond ? "OK  " : "FAIL") + ": " + name);
    if (!cond) ok = false;
}

bool IsValidPdf(byte[] bytes) =>
    bytes.Length > 100 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F';

// ── text + shapes ────────────────────────────────────────────────────────
byte[] textBytes = PdfDocument.Create()
    .WithTitle("AOT Smoke Test")
    .AddPage(PageSizes.A4, canvas =>
    {
        canvas.DrawText("Native AOT smoke test", 72, 72, TextStyle.Default.WithSize(18).WithBold());
        canvas.DrawRectangle(72, 100, 200, 40, ShapeStyle.Filled(PdfColor.LightGray));
    })
    .ToBytes();
Check("text + shapes render", IsValidPdf(textBytes));

// ── table ────────────────────────────────────────────────────────────────
byte[] tableBytes = PdfDocument.Create()
    .AddPage(PageSizes.A4, canvas =>
    {
        var table = new PdfTable(new float[] { 150, 100 })
            .WithHeaderBackground(PdfColor.Gray)
            .WithCellPadding(4f);
        table.AddRow("Column A", "Column B");
        table.AddRow("Value 1", "Value 2");
        canvas.DrawTable(table, 72, 72);
    })
    .ToBytes();
Check("table renders", IsValidPdf(tableBytes));

// ── PdfLayout: Row/Column + flowing text + auto-pagination ─────────────────
var layoutDoc = PdfDocument.Create();
var layout = PdfLayout.Begin(layoutDoc, PageSizes.A4).WithMargins(36);
layout
    .Text("Layout smoke test", TextStyle.Default.WithSize(16).WithBold())
    .Spacer(8)
    .Row(row =>
    {
        row.Column(0.5f, col => col.Text("Left column"));
        row.Column(0.5f, col => col.Text("Right column"));
    })
    .Line()
    .Text(string.Concat(Enumerable.Repeat("Flowing paragraph text that word-wraps. ", 20)));
byte[] layoutBytes = layout.End().ToBytes();
Check("PdfLayout Row/Column + flowing text renders", IsValidPdf(layoutBytes));

// ── AES password protection ─────────────────────────────────────────────
var security = PdfSecurity
    .Protect(userPassword: "user123", ownerPassword: "owner456")
    .WithPermissions(PdfPermissions.Print | PdfPermissions.CopyText);
byte[] encryptedBytes = PdfDocument.Create()
    .WithSecurity(security)
    .WithTitle("Encrypted")
    .AddPage(PageSizes.A4, canvas => canvas.DrawText("Confidential", 72, 72))
    .ToBytes();
Check("AES password protection produces a PDF", IsValidPdf(encryptedBytes));

// ── PKCS#7 digital signature ─────────────────────────────────────────────
using var rsa = RSA.Create(2048);
var certReq = new CertificateRequest(
    "CN=AOT Smoke Test Signer", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
certReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
var selfSigned = certReq.CreateSelfSigned(notBefore, notBefore.AddYears(1));
var pfxBytes = selfSigned.Export(X509ContentType.Pfx);
using var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, (string?)null,
    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

var sigOpts = new PdfSignatureOptions(cert).WithReason("AOT smoke test").WithSignerName("CI");
byte[] signedBytes = PdfDocument.Create()
    .WithSignature(sigOpts)
    .WithTitle("Signed")
    .AddPage(PageSizes.A4, canvas => canvas.DrawText("Digitally signed", 72, 72))
    .ToBytes();
Check("PKCS#7 signature produces a PDF", IsValidPdf(signedBytes));

// ── merge ────────────────────────────────────────────────────────────────
byte[] mergedBytes = new PdfMerger()
    .Add(textBytes)
    .Add(tableBytes)
    .WithTitle("Merged AOT Smoke Test")
    .Merge();
Check("merge produces a PDF", IsValidPdf(mergedBytes));

Console.WriteLine();
if (ok)
{
    Console.WriteLine("AOT-SMOKE-TEST-OK");
    return 0;
}
Console.WriteLine("AOT-SMOKE-TEST-FAILED");
return 1;
