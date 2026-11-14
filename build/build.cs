#:sdk Cake.Sdk
#:property Nullable=disable

// The build pipeline: what CI and a release do, as targets you can also run on a laptop.
//
//   dotnet build/build.cs -- --target=<Target> [--option=value ...]
//
// Run it from the repository root. The Cake.Sdk version is pinned in /global.json ("msbuild-sdks"), where
// Dependabot tracks it. build/Directory.*.props|targets stop the repo-root MSBuild files applying to this script.
//
// The GitHub workflows keep the things only Actions can do (triggers, runner matrix, setup-dotnet, artifact
// upload/download, the test report, service containers, the draft Release, the nuget.org OIDC push) and call
// into this file for everything else.
//
// Targets
//   Info              what this machine would do
//   Version           resolve the release version; writes version/is_release/is_prerelease to $GITHUB_OUTPUT
//   Build             build MajorsilenceReporting.slnx
//   BuildGtkViewer    build MajorsilenceReporting-Linux-GtkViewer.slnx
//   Test              run the unit tests (everything except the Containers category)
//   AllocationGate    fail when rendering allocates >20% more per report than docs/performance/allocation-baseline.json
//   Aot               Native AOT RdlCmd / RdlNative / PdfNative for this OS, smoke-tested and zipped
//                     (--arch=x64|arm64 picks the architecture on Linux; Windows and macOS build both)
//   AotPrecompiledCode  publish + run the build-time <Code> (RdlCodeGen) example under Native AOT (Linux)
//   AotDatabases      the Native AOT report-engine smoke test against live PostgreSQL/MySQL/SQL Server (Linux)
//   WasmPdf | WasmRdl | WasmViewers
//                     browser-wasm publishes (wasm-tools workloads installed first); the first two run under Node
//   PdfValidationSamples  write the signed/encrypted sample PDFs that linux.yml checks with pyHanko
//   TestContainers    the container-backed database tests (needs Docker)
//   Package           the designer/reader/rdlcmd/mapfile zips and the NuGet packages
//
// Zips are written with System.IO.Compression (no 7-Zip). On Linux and macOS that records each file's Unix
// mode, so the executable bit on the self-contained and AOT binaries survives.

var target = Argument ("target", "Info");
var configuration = Argument ("configuration", "Release");
var arch = Argument ("arch", "x64");
var wasmTestFont = Argument ("wasm-test-font", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf");

const string framework = "net10.0";
const string mainSolution = "MajorsilenceReporting.slnx";
const string gtkSolution = "MajorsilenceReporting-Linux-GtkViewer.slnx";

var buildOutput = System.IO.Path.GetFullPath ("Release-Builds/build-output");

// ── Version ────────────────────────────────────────────────────────────────────────────────────────────
// The version is the <Version> in Directory.Build.props, as it always was. A tag is a claim about that
// version, so a tag that disagrees with it fails the run instead of releasing 26.0.3 packages under a
// 26.0.4 name. Tags are v1.2.3 or version_1.2.3.
bool IsTagPush () => EnvironmentVariable ("GITHUB_REF_TYPE") == "tag";

string PropsVersion ()
{
    if (!FileExists ("Directory.Build.props"))
        throw new CakeException ("Directory.Build.props not found: run build/build.cs from the repository root.");
    var doc = System.Xml.Linq.XDocument.Load ("Directory.Build.props");
    // Directory.Build.props has several PropertyGroups; only one carries <Version>.
    return doc.Descendants ("Version").Select (e => e.Value.Trim ()).FirstOrDefault (v => v != "")
        ?? throw new CakeException ("No <Version> in Directory.Build.props.");
}

string ResolveVersion ()
{
    var version = PropsVersion ();
    if (IsTagPush ())
    {
        var tagVersion = EnvironmentVariable ("GITHUB_REF_NAME") ?? "";
        if (tagVersion.StartsWith ("version_")) tagVersion = tagVersion["version_".Length..];
        if (tagVersion.StartsWith ('v')) tagVersion = tagVersion[1..];
        if (tagVersion != version)
            throw new CakeException ($"Tag {EnvironmentVariable ("GITHUB_REF_NAME")} says {tagVersion} but Directory.Build.props says {version}.");
    }
    return version;
}

void WriteActionsOutput (params (string Key, string Value)[] values)
{
    if (EnvironmentVariable ("GITHUB_OUTPUT") is { Length: > 0 } file)
        System.IO.File.AppendAllText (file, string.Concat (values.Select (v => $"{v.Key}={v.Value}\n")));
}

// ── Helpers ────────────────────────────────────────────────────────────────────────────────────────────
// A command whose arguments are passed through exactly as written. Fails the target on a non-zero exit.
void Run (string exe, string args, string workingDirectory = null)
{
    var settings = new ProcessSettings { Arguments = new ProcessArgumentBuilder ().Append (args) };
    if (workingDirectory != null) settings.WorkingDirectory = workingDirectory;
    var code = StartProcess (exe, settings);
    if (code != 0) throw new CakeException ($"'{exe} {args}' exited with code {code}.");
}

// Like Run, but echoes the process output and hands it back, for smoke tests whose verdict is in what they print.
string RunCapture (string exe, string args, string workingDirectory = null)
{
    var settings = new ProcessSettings { Arguments = new ProcessArgumentBuilder ().Append (args), RedirectStandardOutput = true };
    if (workingDirectory != null) settings.WorkingDirectory = workingDirectory;
    var code = StartProcess (exe, settings, out IEnumerable<string> lines);
    var output = lines.ToList ();
    foreach (var line in output) System.Console.WriteLine (line);
    if (code != 0) throw new CakeException ($"'{exe} {args}' exited with code {code}.");
    return string.Join (System.Environment.NewLine, output);
}

string TempDir (string name)
{
    var dir = System.IO.Path.Combine (EnvironmentVariable ("RUNNER_TEMP") ?? System.IO.Path.GetTempPath (), name);
    return Fresh (dir);
}

string OsName () => IsRunningOnWindows () ? "windows" : IsRunningOnMacOs () ? "osx" : "linux";

// Builds a zip from files on disk. Entry names are paths relative to baseDir with '/' separators, so a zip of
// <baseDir>/foo/** has a top-level foo/ folder. Debug symbols (*.pdb, *.dbg) are always left out. On Unix,
// CreateEntryFromFile records the file's mode, which keeps the executable bit.
void Zip (string zipName, IEnumerable<string> roots, Func<string, bool> exclude = null)
{
    var zipPath = System.IO.Path.Combine (buildOutput, zipName);
    if (System.IO.File.Exists (zipPath)) System.IO.File.Delete (zipPath);

    var files = roots
        .SelectMany (r => System.IO.Directory.Exists (r)
            ? System.IO.Directory.EnumerateFiles (r, "*", System.IO.SearchOption.AllDirectories)
            : new[] { r })
        .Select (System.IO.Path.GetFullPath)
        .Distinct ()
        .Where (f => !f.EndsWith (".pdb", StringComparison.OrdinalIgnoreCase) && !f.EndsWith (".dbg", StringComparison.OrdinalIgnoreCase))
        .Select (f => (Path: f, Entry: System.IO.Path.GetRelativePath (buildOutput, f).Replace ('\\', '/')))
        .Where (f => exclude == null || !exclude (f.Entry))
        .OrderBy (f => f.Entry, StringComparer.Ordinal)
        .ToList ();
    if (files.Count == 0) throw new CakeException ($"Nothing to put in {zipName}.");

    using (var archive = System.IO.Compression.ZipFile.Open (zipPath, System.IO.Compression.ZipArchiveMode.Create))
        foreach (var f in files)
            System.IO.Compression.ZipFileExtensions.CreateEntryFromFile (archive, f.Path, f.Entry, System.IO.Compression.CompressionLevel.Optimal);
    Information ($"zipped {files.Count} files -> {zipName}");
}

// What `Copy-Item <dir> -Destination <existingDir> -Recurse` did: the directory lands INSIDE destParent,
// under its own name. The release zips have always had that layout (e.g. linux-x64/publish/...).
string CopyInto (string sourceDir, string destParent)
{
    if (!DirectoryExists (sourceDir)) throw new CakeException ($"Expected build output is missing: {sourceDir}");
    var dest = System.IO.Path.Combine (destParent, System.IO.Path.GetFileName (sourceDir.TrimEnd ('/', '\\')));
    EnsureDirectoryExists (dest);
    CopyDirectory (sourceDir, dest);
    return dest;
}

string Fresh (string dir)
{
    if (DirectoryExists (dir)) DeleteDirectory (dir, new DeleteDirectorySettings { Recursive = true, Force = true });
    EnsureDirectoryExists (dir);
    return dir;
}

void Publish (string project, string rid, bool aot, string outputDir = null)
{
    var msbuild = new DotNetMSBuildSettings ().WithProperty ("GeneratePackageOnBuild", "false");
    if (aot) msbuild.WithProperty ("PublishAot", "true");
    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Framework = framework,
        Runtime = rid,
        SelfContained = true,
        MSBuildSettings = msbuild,
    };
    if (outputDir != null) settings.OutputDirectory = outputDir;
    DotNetPublish (project, settings);
}

// ── Setup ──────────────────────────────────────────────────────────────────────────────────────────────
Task ("Info")
    .Does (() =>
{
    Information ($"OS:      {OsName ()}");
    Information ($"Version: {PropsVersion ()} (tag push: {IsTagPush ()})");
});

Task ("Version")
    .Does (() =>
{
    var version = ResolveVersion ();
    var isRelease = IsTagPush ().ToString ().ToLowerInvariant ();
    // A SemVer pre-release label ("27.0.0-alpha.1") marks the GitHub Release as a pre-release.
    var isPrerelease = version.Contains ('-').ToString ().ToLowerInvariant ();
    Information ($"version={version} is_release={isRelease} is_prerelease={isPrerelease}");
    WriteActionsOutput (("version", version), ("is_release", isRelease), ("is_prerelease", isPrerelease));
});

// ── Build / test ───────────────────────────────────────────────────────────────────────────────────────
Task ("Build")
    .Does (() =>
{
    DotNetBuild (mainSolution, new DotNetBuildSettings { Configuration = configuration });
});

Task ("BuildGtkViewer")
    .Does (() =>
{
    DotNetBuild (gtkSolution, new DotNetBuildSettings { Configuration = configuration });
});

// The Containers category needs Docker and runs from containers.yml.
Task ("Test")
    .Does (() =>
{
    DotNetTest (mainSolution, new DotNetTestSettings
    {
        Configuration = configuration,
        Filter = "TestCategory!=Containers",
        Verbosity = DotNetVerbosity.Normal,
        Collectors = new[] { "XPlat Code Coverage" },
        Loggers = new[] { "trx" },
    });
});

// Writes signed and encrypted+signed PDFs into $MAJORSILENCE_PDF_VALIDATION_DIR for an independent validator
// (pyHanko, in linux.yml). Majorsilence.Pdf's own reader and writer can agree on a mistake every other reader
// rejects (#363), so the unit tests alone are not enough.
Task ("PdfValidationSamples")
    .Does (() =>
{
    DotNetTest ("Majorsilence.Pdf.Tests/Majorsilence.Pdf.Tests.csproj", new DotNetTestSettings
    {
        Configuration = configuration,
        Framework = framework,
        Filter = "TestCategory=PdfValidationSamples",
        Verbosity = DotNetVerbosity.Normal,
    });
});

// Fails when rendering allocates over 20% more bytes per report than docs/performance/allocation-baseline.json.
// Intended increase: dotnet run -c Release -f net10.0 --project Majorsilence.Reporting.Benchmarks -- gate --update
Task ("AllocationGate")
    .Does (() =>
{
    Run ("dotnet", $"run -c {configuration} -f {framework} --no-build --project Majorsilence.Reporting.Benchmarks -- gate");
});

// ── Native AOT ─────────────────────────────────────────────────────────────────────────────────────────
// RdlCmd, RdlNative (rdlnative shared library) and PdfNative (pdfnative shared library) published with
// PublishAot. AOT cannot cross-compile across OS families, so each OS builds its own: Linux one architecture
// per run (--arch), Windows and macOS both. Before anything is zipped the AOT binaries are proven to run:
// the Majorsilence.Pdf and report-engine smoke tests are published as AOT and executed, and the AOT RdlCmd
// renders a SQLite report. Only the runner's own architecture can execute, so that is what gets smoke-tested.
Task ("Aot")
    .IsDependentOn ("Version")
    .Does (() =>
{
    var os = OsName ();
    var version = PropsVersion ();
    var exeSuffix = IsRunningOnWindows () ? ".exe" : "";

    if (os == "linux" && arch != "x64" && arch != "arm64")
        throw new CakeException ($"--arch must be x64 or arm64, got '{arch}'.");
    var rids = os switch
    {
        "linux" => new[] { $"linux-{arch}" },
        "windows" => new[] { "win-x64", "win-arm64" },
        _ => new[] { "osx-x64", "osx-arm64" },
    };
    var nativeArm = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
    var smokeRid = os switch { "linux" => rids[0], "windows" => "win-x64", _ => nativeArm ? "osx-arm64" : "osx-x64" };

    if (IsRunningOnWindows ())
        DotNetBuild (mainSolution, new DotNetBuildSettings { Configuration = configuration, MSBuildSettings = new DotNetMSBuildSettings ().WithProperty ("GeneratePackageOnBuild", "false") });

    string AotDir (string project, string rid) => $"{project}/bin/{configuration}/{framework}/{rid}-aot/publish";
    foreach (var project in new[] { "RdlCmd", "RdlNative", "PdfNative" })
        foreach (var rid in rids)
            Publish (project, rid, aot: true, outputDir: AotDir (project, rid));

    // Native AOT verification: run the smoke tests as real AOT binaries, not just check that they compiled.
    foreach (var smoke in new[] { "PdfAotSmokeTest", "RdlAotSmokeTest" })
    {
        var outDir = $"Examples/{smoke}/bin/{configuration}/{framework}/{smokeRid}/publish";
        Publish ($"Examples/{smoke}", smokeRid, aot: true, outputDir: outDir);
        Information ($"Running {smoke} ({smokeRid})...");
        Run (System.IO.Path.GetFullPath ($"{outDir}/{smoke}{exeSuffix}"), "");
    }

    // The AOT RdlCmd must run a report against a real database: drivers are found by RegisterDataProvider
    // under AOT (RdlCmd/AotDataProviders.cs), not by loading their assemblies.
    var checkDir = Fresh (System.IO.Path.Combine (System.IO.Path.GetTempPath (), $"rdlcmd-aot-check-{Guid.NewGuid ():N}"));
    try
    {
        CopyFile ("ReportTests/Reports/PushedDataCalculatedFieldTest.rdl", System.IO.Path.Combine (checkDir, "PushedDataCalculatedFieldTest.rdl"));
        CopyFile ("Examples/northwindEF.db", System.IO.Path.Combine (checkDir, "northwindEF.db"));
        Run (System.IO.Path.GetFullPath ($"{AotDir ("RdlCmd", smokeRid)}/RdlCmd{exeSuffix}"),
            $"\"/fPushedDataCalculatedFieldTest.rdl\" \"/tcsv\" \"/o{checkDir}\"", checkDir);
        var csv = System.IO.File.ReadAllText (System.IO.Path.Combine (checkDir, "PushedDataCalculatedFieldTest.csv"));
        if (!csv.Contains ("ALFKI"))
            throw new CakeException ($"AOT RdlCmd ran but the SQLite report has no data: {csv}");
        Information ($"AOT RdlCmd SQLite report check passed ({smokeRid}).");
    }
    finally
    {
        if (DirectoryExists (checkDir)) DeleteDirectory (checkDir, new DeleteDirectorySettings { Recursive = true, Force = true });
    }

    // Stage <folder>/<rid>/publish/... (+ the C header for the shared libraries) and zip it.
    EnsureDirectoryExists (buildOutput);
    var products = new[]
    {
        (Project: "RdlCmd", Folder: "majorsilence-reporting-rdlcmd-aot", Header: (string) null),
        (Project: "RdlNative", Folder: "majorsilence-reporting-rdlnative", Header: "RdlNative/rdlnative.h"),
        (Project: "PdfNative", Folder: "majorsilence-pdfnative", Header: "PdfNative/pdfnative.h"),
    };
    foreach (var p in products)
    {
        var folder = Fresh (System.IO.Path.Combine (buildOutput, p.Folder));
        foreach (var rid in rids)
            CopyInto (AotDir (p.Project, rid), Fresh (System.IO.Path.Combine (folder, rid)));
        if (p.Header != null)
            CopyFile (p.Header, System.IO.Path.Combine (folder, System.IO.Path.GetFileName (p.Header)));

        // Linux: one zip per architecture. Windows: one zip per architecture, each with just its own folder
        // (and the header). macOS: one zip holding both.
        if (os == "osx")
            Zip ($"{version}-{p.Folder}-osx.zip", new[] { folder });
        else
            foreach (var rid in rids)
            {
                var a = rid.Split ('-')[1];
                var roots = new List<string> { System.IO.Path.Combine (folder, rid) };
                if (os == "windows" && p.Header != null) roots.Add (System.IO.Path.Combine (folder, System.IO.Path.GetFileName (p.Header)));
                if (os == "linux") roots = new List<string> { folder };
                Zip ($"{version}-{p.Folder}-{os}-{a}.zip", roots);
            }
    }
});

// ── Release zips and NuGet packages ────────────────────────────────────────────────────────────────────
// The designer/reader/rdlcmd/mapfile zips and every NuGet package. Every project is plain net8.0/net10.0 and
// the self-contained RdlCmd builds are ordinary (non-AOT) publishes, so none of this needs Windows. It clears
// build-output when it starts, which is why it is its own job and not a step in front of an AOT build.
Task ("Clean")
    .Does (() =>
{
    foreach (var pattern in new[] { "bin", "obj", "build-output" })
        foreach (var dir in GetDirectories ($"./**/{pattern}", new GlobberSettings { Predicate = d => !d.Path.FullPath.Contains ("/.git/") && !d.Path.FullPath.Contains ("/node_modules/") }))
            if (DirectoryExists (dir)) DeleteDirectory (dir, new DeleteDirectorySettings { Recursive = true, Force = true });
});

Task ("Package")
    .IsDependentOn ("Version")
    .IsDependentOn ("Clean")
    .Does (() =>
{
    var version = PropsVersion ();
    var noPackOnBuild = new DotNetMSBuildSettings ().WithProperty ("GeneratePackageOnBuild", "false");
    DotNetRestore (mainSolution);
    DotNetBuild (mainSolution, new DotNetBuildSettings { Configuration = configuration, Verbosity = DotNetVerbosity.Minimal, MSBuildSettings = noPackOnBuild });

    string[] selfContainedRids = ["linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x64", "win-arm64"];
    foreach (var rid in selfContainedRids)
        Publish ("RdlCmd", rid, aot: false);

    string Built (string project) => $"{project}/bin/{configuration}/{framework}";
    var dirs = new Dictionary<string, string>();
    foreach (var name in new[] { "designer", "mapfile", "reader", "rdlcmd" })
        dirs[name] = Fresh (System.IO.Path.Combine (buildOutput, $"majorsilence-reporting-{name}-{framework}-anycpu"));
    var selfContained = Fresh (System.IO.Path.Combine (buildOutput, "majorsilence-reporting-rdlcmd-self-contained"));

    CopyInto (Built ("ReportDesigner"), dirs["designer"]);
    CopyFile ("RdlDesign/App.ico", System.IO.Path.Combine (dirs["designer"], "App.ico"));
    CopyInto (Built ("RdlCmd"), dirs["rdlcmd"]);
    CopyInto (Built ("RdlReader"), dirs["reader"]);
    CopyInto (Built ("RdlMapFile"), dirs["mapfile"]);
    foreach (var rid in selfContainedRids)
        CopyInto ($"{Built ("RdlCmd")}/{rid}/publish", Fresh (System.IO.Path.Combine (selfContained, rid)));

    foreach (var name in new[] { "designer", "mapfile", "reader" })
        Zip ($"{version}-majorsilence-reporting-{name}-{framework}-anycpu.zip", new[] { dirs[name] });
    // The anycpu RdlCmd zip leaves out the Windows self-contained publishes (they ship in the self-contained zip).
    Zip ($"{version}-majorsilence-reporting-rdlcmd-{framework}-anycpu.zip", new[] { dirs["rdlcmd"] },
        entry => entry.Contains ($"/{framework}/win-arm64/") || entry.Contains ($"/{framework}/win-x64/"));
    Zip ($"{version}-majorsilence-reporting-rdlcmd-self-contained.zip", new[] { selfContained });

    DotNetBuild (mainSolution, new DotNetBuildSettings { Configuration = configuration, Verbosity = DotNetVerbosity.Minimal, MSBuildSettings = noPackOnBuild });
    DotNetPack (mainSolution, new DotNetPackSettings { Configuration = configuration, NoBuild = true, OutputDirectory = buildOutput });

    Information ("Publish nuget packages with:");
    Information ($"  dotnet nuget push {buildOutput}/*.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json --skip-duplicate");
    Information ($"  dotnet nuget push {buildOutput}/*.snupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json --skip-duplicate");
});

// Report <Code> blocks are compiled at build time (RdlCodeGen) and then run natively.
Task ("AotPrecompiledCode")
    .WithCriteria (() => IsRunningOnLinux (), "the example is a linux-x64 publish")
    .Does (() =>
{
    var outDir = TempDir ("aotcode");
    DotNetPublish ("Examples/AotPrecompiledCode/App/App.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = "linux-x64",
        OutputDirectory = outDir,
    });
    Run (System.IO.Path.Combine (outDir, "AotPrecompiledCode"), "", outDir);
    var pdf = new System.IO.FileInfo (System.IO.Path.Combine (outDir, "score-card.pdf"));
    if (!pdf.Exists || pdf.Length == 0) throw new CakeException ("AotPrecompiledCode produced no score-card.pdf.");
});

// Runs the Native AOT smoke test against real PostgreSQL, MySQL and SQL Server servers (service containers in
// aot-databases.yml), so the drivers registered with RdlEngineConfig.RegisterDataProvider are exercised end to
// end as an AOT binary, not just constructed. The connection strings arrive as RDL_SMOKE_* environment
// variables. The same smoke test runs without servers as part of the Aot target.
Task ("AotDatabases")
    .WithCriteria (() => IsRunningOnLinux (), "the smoke test is a linux-x64 publish")
    .Does (() =>
{
    var outDir = TempDir ("aot-smoke");
    Publish ("Examples/RdlAotSmokeTest", "linux-x64", aot: true, outputDir: outDir);
    var log = RunCapture (System.IO.Path.Combine (outDir, "RdlAotSmokeTest"), "");
    // A skipped database means the connection string never reached the test; that must not pass silently.
    if (log.Contains ("  skip  ")) throw new CakeException ("a live database scenario was skipped");
});

// ── Browser wasm ───────────────────────────────────────────────────────────────────────────────────────
// The wasm apps target net10.0, but the libraries they reference also target net11.0, so the 11.0 SDK is
// required. Under the 11.0 SDK, `wasm-tools` alone relinks net10.0 apps with .NET 11's Emscripten, which
// SkiaSharp.NativeAssets.WebAssembly doesn't match (libSkiaSharp fails to load); `wasm-tools-net10` supplies
// the .NET 10 Emscripten toolchain for net10.0 apps.
Task ("WasmWorkloads")
    .Does (() => Run ("dotnet", "workload install wasm-tools wasm-tools-net10"));

// Publishes Majorsilence.Pdf + Majorsilence.Pdf.Security as a browser-wasm app and runs it under Node (which
// must be on PATH), the same way the Native AOT smoke test proves the AOT build actually runs.
Task ("WasmPdf")
    .IsDependentOn ("WasmWorkloads")
    .Does (() =>
{
    DotNetPublish ("Examples/PdfWasmSmokeTest", new DotNetPublishSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetMSBuildSettings ().WithProperty ("WasmTestFont", wasmTestFont),
    });
    var log = RunCapture ("node", "Examples/PdfWasmSmokeTest/run-node.mjs");
    if (!log.Contains ("AOT-SMOKE-TEST-OK")) throw new CakeException ("The Pdf wasm smoke test did not report AOT-SMOKE-TEST-OK.");
});

// Renders reports (expressions, grouping, charts, PDF/HTML/Excel/TIFF, file-based data providers) with
// RdlEngine published as browser-wasm and run under Node.
Task ("WasmRdl")
    .IsDependentOn ("WasmWorkloads")
    .Does (() =>
{
    DotNetPublish ("Examples/RdlWasmSmokeTest", new DotNetPublishSettings { Configuration = configuration });
    var log = RunCapture ("node", "Examples/RdlWasmSmokeTest/run-node.mjs");
    if (!log.Contains ("RDL-WASM-SMOKE-TEST-OK")) throw new CakeException ("The RDL wasm smoke test did not report RDL-WASM-SMOKE-TEST-OK.");
});

// The two browser viewer hosts (Avalonia report viewer, Majorsilence.Forms report viewer) must publish.
Task ("WasmViewers")
    .IsDependentOn ("WasmWorkloads")
    .Does (() =>
{
    DotNetPublish ("Majorsilence.Reporting.UI.Browser", new DotNetPublishSettings { Configuration = configuration });
    DotNetPublish ("Examples/Hosting/Browser", new DotNetPublishSettings { Configuration = configuration });
});

// ── Container-backed database tests ────────────────────────────────────────────────────────────────────
// Off by default: the regular test runs exclude the "Containers" category, this runs only that category, and
// only from containers.yml by hand. Needs a Docker daemon and REPORTING_CONTAINER_TESTS=1.
Task ("TestContainers")
    .Does (() =>
{
    const string project = "ReportTests.Containers/ReportTests.Containers.csproj";
    DotNetBuild (project, new DotNetBuildSettings { Configuration = configuration });
    DotNetTest (project, new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        Filter = "TestCategory=Containers",
        Verbosity = DotNetVerbosity.Normal,
        Loggers = new[] { "trx" },
    });
});

RunTarget (target);
