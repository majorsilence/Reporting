# Package aliases

Compatibility metapackages. Before Majorsilence Reporting became SkiaSharp-only
(see `../MIGRATION-NOTES.md` — "Retire the System.Drawing / SkiaSharp split"), the
cross-platform SkiaSharp render path shipped under a separate package ID with a
`.SkiaSharp` suffix, alongside a `System.Drawing`-based package under the base ID.

That split is gone. The base IDs — `Majorsilence.Reporting.RdlEngine`,
`.RdlCreator`, `.RdlCri`, `.DataProviders` — **are** the SkiaSharp build now.

Each project here publishes the old `.SkiaSharp` ID as a thin metapackage: no
assemblies, just a dependency on the matching base package at the same version, so
existing `<PackageReference Include="Majorsilence.Reporting.*.SkiaSharp" />` lines
keep resolving. New code should reference the base package directly.

| Alias package                             | Forwards to                        |
|-------------------------------------------|------------------------------------|
| `Majorsilence.Reporting.RdlEngine.SkiaSharp`     | `Majorsilence.Reporting.RdlEngine`     |
| `Majorsilence.Reporting.RdlCreator.SkiaSharp`    | `Majorsilence.Reporting.RdlCreator`    |
| `Majorsilence.Reporting.RdlCri.SkiaSharp`        | `Majorsilence.Reporting.RdlCri`        |
| `Majorsilence.Reporting.DataProviders.SkiaSharp` | `Majorsilence.Reporting.DataProviders` |
