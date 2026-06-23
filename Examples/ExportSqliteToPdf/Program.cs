// ExportSqliteToPdf — minimal example: query a SQLite database and write a PDF.
//
// Run:  dotnet run
// Output: products.pdf (open) and products-protected.pdf (password: "user") in the output dir
//
// Key patterns shown:
//   - RdlEngineConfig.RdlEngineConfigInit() called once at startup
//   - RDLParser takes the RDL XML as a plain string — any source works
//   - The database path is injected into the RDL XML before parsing so
//     the engine can validate the schema at parse time
//   - PdfSecurity.Protect() applies AES-256 password encryption + permissions

using Majorsilence.Pdf.Security;
using Majorsilence.Reporting.Rdl;

RdlEngineConfig.RdlEngineConfigInit();

var baseDir = AppContext.BaseDirectory;
var rdlPath = Path.Combine(baseDir, "Products.rdl");
var dbPath  = Path.Combine(baseDir, "sqlitetestdb2.db");
var outPath = Path.Combine(baseDir, "products.pdf");
var outSecurePath = Path.Combine(baseDir, "products-protected.pdf");

// Inject the actual database path before parsing so the engine can
// validate the schema.  The RDL file stores a relative placeholder.
var rdlXml = (await File.ReadAllTextAsync(rdlPath))
    .Replace("sqlitetestdb2.db", dbPath);

var rdlp = new RDLParser(rdlXml) { Folder = baseDir };
using var report = await rdlp.Parse();

if (report.ErrorMaxSeverity > 4)
{
    Console.Error.WriteLine("Report parse errors:");
    foreach (var err in report.ErrorItems)
        Console.Error.WriteLine($"  {err}");
    return 1;
}

// ── Unprotected export ────────────────────────────────────────────────────
await report.RunGetData();
var ofs = new OneFileStreamGen(outPath, true);
await report.RunRender(ofs, OutputPresentationType.PDF);
Console.WriteLine($"Written: {outPath}");

// ── Password-protected export ─────────────────────────────────────────────
// PdfSecurity.Protect() defaults to AES-256 (V5/R6).
// The owner password is optional; omit it to set only a user password.
var security = PdfSecurity
    .Protect(userPassword: "user", ownerPassword: "owner")
    .WithPermissions(PdfPermissions.Print | PdfPermissions.CopyText);

await report.RunGetData();
var ofsSecure = new OneFileStreamGen(outSecurePath, true);
await report.RunRender(ofsSecure, OutputPresentationType.PDF, security);
Console.WriteLine($"Written: {outSecurePath}  (user password: \"user\")");

return 0;
