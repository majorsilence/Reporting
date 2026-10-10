using Majorsilence.Reporting.RdlCodeGen;

// RdlCodeGen --output <file.vb> <report.rdl | @listfile>...
string? output = null;
var reports = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length)
        output = args[++i];
    else if (args[i].StartsWith('@'))
        reports.AddRange(File.ReadAllLines(args[i][1..]).Where(l => !string.IsNullOrWhiteSpace(l)));
    else
        reports.Add(args[i]);
}
if (output == null)
{
    Console.Error.WriteLine("usage: RdlCodeGen --output <file.vb> <report.rdl | @listfile>...");
    return 2;
}

var gen = new Generator();
var blocks = gen.Collect(reports);
string code = gen.Generate(blocks);
foreach (string m in gen.Messages)
    Console.WriteLine(m);
if (gen.HasErrors)
    return 1;

// Only touch the file when it changes so incremental builds stay incremental.
if (!File.Exists(output) || File.ReadAllText(output) != code)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    File.WriteAllText(output, code);
}
Console.WriteLine($"RdlCodeGen: {blocks.Count} code block(s) from {reports.Count} report(s) -> {output}");
return 0;
