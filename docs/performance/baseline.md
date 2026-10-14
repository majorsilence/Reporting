# RdlEngine performance baseline

Taken on `main` after PR #435. net10.0, BenchmarkDotNet 0.15.8 `--job short` (3 iterations; use for ranking, allocated bytes are exact, timings have wide error).
Machine: Intel Core i5-8350U (4 cores / 8 threads), Ubuntu 26.04, .NET 10.0.12, workstation concurrent GC, TieredPGO default.
Raw BDN tables: `raw/*-net10.md` (before) and `raw/*-after-net10.md` (after the changes in "Results" below). Reproduce:

```
dotnet run -c Release -f net10.0 --project Majorsilence.Reporting.Benchmarks -- \
  --filter '*ParseBenchmarks*' '*GetDataBenchmarks*' '*RenderBenchmarks*' '*EndToEndBenchmarks*' '*LargeRowsBenchmarks*' '*ConcurrencyBenchmarks*' --job short
dotnet run -c Release -f net10.0 --project Majorsilence.Reporting.Benchmarks -- smoke   # output size + hash per workload/format
```

## Single report, per stage
| Workload | Parse | RunGetData | Render PDF | Render HTML | End-to-end PDF (parse+data+render) |
|---|---|---|---|---|---|
| testdata (tiny) | 170 us / 126 KB | n/a | 5.5 ms / **2.3 MB** | 0.14 ms / 231 KB | 6.9 ms / 2.39 MB |
| WorldFacts (xml rows) | 300 us / 208 KB | 0.85 ms / 541 KB | **66 ms / 16.7 MB** | 7.9 ms / 6.2 MB | 100 ms / 17.1 MB |
| MatrixExample (sqlite) | 495 us / 375 KB | 0.17 ms / 117 KB | 8.1 ms / 4.1 MB | 1.0 ms / 1.5 MB | 9.2 ms / 4.4 MB |
| ChartTypes | 1.86 ms / 1.0 MB | n/a | 24 ms / 3.9 MB | 36.7 ms / 4.7 MB | n/a |
| ListPagingTest (html text, post-#435) | n/a | n/a | **119 ms / 17.0 MB** | 41 ms / 1.1 MB | n/a |
| CanGrowSplitTest | n/a | n/a | 9.6 ms / 1.4 MB | 14 us / 39 KB | n/a |
| ImageSizing | n/a | n/a | 1.4 ms / 294 KB | 4 us / 11 KB | n/a |

Other formats, WorldFacts: CSV 5.7 ms / 3.0 MB, XML 8.6 ms / 4.7 MB, RTF 8.0 ms / 6.8 MB, Excel2007 15.4 ms / 8.7 MB.

## Many rows (SetData, one table, PDF, parse+data+render)
| Rows | Time | Allocated | Per row |
|---|---|---|---|
| 1,000 | 60 ms | 20.5 MB | ~20 KB |
| 10,000 | 545 ms | 189 MB | ~19 KB |
| 100,000 | 5.35 s | 1.89 GB (Gen2 x3000/1000 ops) | ~19 KB |

Linear, but ~19 KB and ~54 us per row of four cells is high.

## Concurrency (16 requests per op, each its own Report, parse+data+render PDF; time is per request)
| Workload | 1 thread | 4 threads | 8 threads | 16 threads |
|---|---|---|---|---|
| testdata | 6.5 ms | 3.3 ms | 3.4 ms | 3.5 ms |
| WorldFacts | 66 ms | 33 ms | 37 ms | 39 ms |

## Observations (to confirm in Phase 2)
1. **Fixed cost per PDF is large.** An almost empty report (testdata) costs 5.5 ms and 2.3 MB with **Gen2 collections at 250 per 1000 ops**, i.e. large-object-heap allocations on every render. The audit points at per-report font loading (`FontRegistry` + `File.ReadAllBytes` + TTF parse, `RenderPdf_Raw.cs` ~440-500, `Majorsilence.Pdf/Internal/FontCache.cs`).
2. **Scaling stops at the physical core count**: ~2x at 4 threads, then flat or worse. Allocation volume and GC (Gen1/Gen2 per op) are the likely limit; "Completed Work Items" for WorldFacts is 3,700 per op, so async state machines / `Task` use is heavy too.
3. **Text layout dominates large documents**: WorldFacts PDF 66 ms vs HTML 8 ms; ListPagingTest PDF 119 ms vs HTML 41 ms.
4. **Per-row cost** in tables is ~19 KB (boxing, `Row`/`object[]`, text measurement, page items).
5. Render benchmarks re-render one `Report`; tiny HTML numbers (CanGrowSplit, ImageSizing) suggest cached state between iterations, so prefer the end-to-end and concurrency numbers for decisions.

## Notes
- Output determinism for before/after checks: PDF (after masking date/ID), CSV and XML hash identically between runs. HTML, RTF and XLSX embed volatile data, so compare those by size/content rather than hash.
- The old numbers in the repo Readme (macOS/Windows, `JsonDataProviderBenchmark`) are from a different machine and benchmark; the Readme still shows them.

## Results of the first round of changes

Same machine and commands as above, `--job short`. Allocation numbers are exact; times carry the usual short-job error. net8.0 could not be measured here (only the .NET 10 runtime is installed), the changes do not depend on the runtime version.

| Workload (parse + data + PDF) | Before | After | Change |
|---|---|---|---|
| testdata (tiny report) | 6.9 ms, 2.39 MB | 3.4 ms, 0.87 MB | -51% time, -64% memory |
| MatrixExample | 9.2 ms, 4.44 MB | 6.0 ms, 2.63 MB | -35% time, -41% memory |
| WorldFacts | 100 ms (noisy), 17.1 MB | 44 ms, 9.3 MB | about -55% time, -46% memory |
| ListPagingTest, render only | 119 ms, 17.0 MB | 113 ms, 14.1 MB | -5% time, -15% memory |
| 10,000-row table | 545 ms, 189 MB | 412 ms, 102 MB | -24% time, -46% memory |
| 100,000-row table | 5.35 s, 1.89 GB | 4.12 s, 1.01 GB | -23% time, -46% memory |

Gen2 collections for the tiny report went from 250 to 0 per 1000 renders.

Concurrent requests (time per request, parse + data + PDF):

| Workload | Threads | Before | After |
|---|---|---|---|
| testdata | 1 / 4 / 8 / 16 | 6.5 / 3.3 / 3.4 / 3.5 ms | 3.4 / 1.9 / 1.4 / 2.1 ms |
| WorldFacts | 1 / 4 / 8 / 16 | 66 / 33 / 37 / 39 ms | 43 / 28 / 27 / 29 ms |

### What changed (one commit each)
1. **Shared font registry and parsed fonts** - every PDF re-walked the font folders and re-read and re-parsed each font file. Now once per process (`RenderPdf_Raw.BuildFontRegistry`, `Majorsilence.Pdf/Internal/FontCache.cs`). Fonts installed after the first report are seen after a restart, as with `InstalledFontScanner` already.
2. **Text measuring** (`RenderBase.MeasureString`) - a Font and StringFormat per text item, regions never disposed (Skia finalizers), font disposed twice instead of the format. Now reused per render and disposed.
3. **PDF content stream** - numbers appended without a string each (`AppendNum`), pages compressed straight from the builder's chunks, glyph ids appended without a string per glyph.
4. **zlib level 6 instead of 9** - PDFs within 0.3% of the same size, about 20% less render time. PDF bytes differ from before (they inflate to the same content).
5. **Per-render style caches** - `TextStyle`, `StrokeStyle`, `ShapeStyle` built once per distinct value, not per text item or border.
6. **Rewrap fast paths** - `RejoinSoftWraps` / `RewrapLines` return their input when nothing needs changing.
7. **Thread safety** (see `thread-safety-audit.md`) - config init, MHT entity table, shared `HttpClient` headers, name counters, and `Graphics.MeasureString` (see below).

PDF, CSV and XML output was byte-identical across steps 1-3, 5, 6 (checked with `-- smoke` hashes); step 4 changes the compressed bytes only. All tests pass: ReportTests 574, Majorsilence.Pdf 306, RdlAsp.Mvc 16, RdlCreator 24.

### Thread safety finding in a dependency
`Graphics.MeasureString` in `Majorsilence.Forms` goes through `TextMeasurer`, whose static `Dictionary` cache was corrupted by concurrent use (a 16-thread run failed with "Operations that change non-concurrent collections must have exclusive access"; the baseline code path had the same race). Fixed in `Majorsilence.Forms` (`TextMeasurer.cs`, with a regression test that reproduces the exception without the fix). The fix shipped in Majorsilence.Forms 26.11.3, and the engine's temporary lock (`MeasureStringLocked`) is gone. The engine now measures single lines with `Graphics.MeasureRunWidth` / `MeasureRunSpans` (same floats as `MeasureCharacterRanges`, no Region per cell).

## Round 2 (allocation, KB per render; timings not re-measured on mains power)
| Workload | Before | After |
|---|---:|---:|
| WorldFacts PDF | 7,958 | 5,345 |
| WorldFacts CSV | 2,299 | 1,461 |
| WorldFacts HTML | 5,349 | 2,458 |
| WorldFacts XML | 3,645 | 2,619 |
| WorldFacts RTF | 6,119 | 3,787 |
| WorldFacts Excel | 8,130 | 6,027 |
| testdata PDF | 816 | 705 |
| large:1000 PDF | 6,983 | 5,499 |
| large:10000 PDF | 104,000 | ~50,000 |

Thread-pool work items per WorldFacts render: 15 -> 0. Output is unchanged in every format (normalised smoke hashes). What changed: shared constant `StyleInfo` (copy on write), struct cache keys, completed tasks for constant expressions, synchronous fast paths for plain fields and cell text, synchronous data-file read, escape fast paths, HTML CSS class cache, lazy Excel builder objects, `Textbox` per-row cache moved to `TextboxRuntime` (fixes a data race on shared definitions), PDF text path (no per-cell tuple list, remembered coverage and widths), per-render measurement cache, and `TrueTypeFont.Subset` without intermediate arrays.

What is left is mostly the PDF content-stream builder's growth, `MeasureCharacterRanges` (the Forms `MeasureRunWidth` API, once released), the finished-font-object cache (needs the approved base-Latin glyph set), and `CompiledReport`.

## CompiledReport (parse once, run many)
`CompiledReport.CompileAsync(rdl, options)` parses a definition once; `CreateReport()` makes a per-run `Report` that shares it and is safe to use from any thread (one thread per Report, as always). Disposing such a report leaves the definition alone. `CompiledReportBenchmarks` (PDF, in-process data, so no database schema queries): testdata 1.45 -> 1.17 ms, MatrixExample 4.5 -> 3.5 ms, WorldFacts 35.9 -> 29.2 ms per request; allocation -5% to -21%. A real database adds its schema queries to every parse, so the saving there is larger.

Shared-definition hazards fixed for this: `Report.Dispose` no longer disposes a definition it does not own; the report log is locked; a `.dsr` data source reference publishes its connection properties only once built; the data source password is fixed at compile time (setting it on a shared report throws `InvalidOperationException`; subreports no longer write it into their definition); the Textbox per-row cache lives in the per-run state (round 2). `Subreport.ContainerOffset` is still written during a run, with the same value every time. `CompiledReportTests` renders nine reports (two with subreports) in four formats from one shared definition on eight threads and compares with a fresh parse. RdlAsp.Mvc now caches a `CompiledReport` instead of a bare definition (which the first request's `Report.Dispose` could have emptied).

## Round 4 (parse + data + render, per render)
Same machine; `-- profile <workload> <format> 8`, `main` and this branch run alternately, so the timings share the
machine's state (they still move by about 10% between runs; allocation is exact). Targets were picked from
allocation stacks (`dotnet-trace --profile gc-verbose`) and thread-time samples, not guessed.

| Workload | Before | After |
|---|---:|---:|
| ListPagingTest PDF (html text) | 40.8 ms, 13,886 KB | 12.4 ms, 6,847 KB |
| WorldFacts PDF | 27.0 ms, 4,689 KB | 25.8 ms, 3,080 KB |
| 1,000-row table PDF | 16.5 ms, 4,446 KB | 15.8 ms, 2,410 KB |
| 10,000-row table PDF | 180 ms, 40.7 MB | 148 ms, 21.0 MB |
| testdata PDF | 1.41 ms, 678 KB (gen1/gen2 42/42 per 1000) | 1.30 ms, 445 KB (0/0) |
| MatrixExample PDF | 5.2 ms, 2,121 KB | 5.4 ms, 1,788 KB |
| CanGrowSplitTest PDF | 0.89 ms, 436 KB | 0.86 ms, 346 KB |
| WorldFacts Excel | 13.3 ms, 5,919 KB | 11.5 ms, 3,915 KB |
| WorldFacts RTF | 5.7 ms, 3,788 KB (gen2 286) | 4.8 ms, 3,039 KB (gen2 154) |
| WorldFacts HTML | 3.6 ms, 2,455 KB | 3.2 ms, 1,856 KB |
| WorldFacts CSV | 2.4 ms, 1,463 KB | 2.3 ms, 1,321 KB |
| WorldFacts XML | 2.7 ms, 2,621 KB | 2.6 ms, 2,598 KB |

What changed (one commit each; output unchanged in every format by the normalised smoke hashes):
1. **HTML text split over pages**: the lexer returns words and tags as pieces of the source, text runs between two
   tags share one StyleInfo, the split points share their tag strings. Checked against the old lexer on 500,000
   generated inputs.
2. **Font cell metrics kept for the process**: `FontFamily.GetCellDescent` (Majorsilence.Forms 26.11.3) asks fontconfig
   for the typeface on every call; it was 54% of ListPagingTest's time.
3. **PDF content builder reused**: a page is compressed when the next is added and its builder serves the next page
   (and, through one static slot, the next document). Pages are held compressed instead of as UTF-16 text.
4. **PDF save**: stream objects written in parts, the buffer sized once, one write to the destination.
5. **Table cells**: no line split for a cell that fits; a numeric General-aligned cell reuses one right-aligned style.
6. **Constant style**: one completed `Task<StyleInfo>` instead of one per item.
7. **Excel**: one cell format per shared style; style index kept per format.
8. **Growing cells**: the font, string formats, line height and paddings of a constant style are made once per run
   for measuring a CanGrow textbox (`Style.GetMeasureSetup`); WorldFacts PDF 3,080 -> 2,515 KB, Excel 3,915 -> 3,383 KB
   and about 20% faster. `GrowHeightTests` checks it measures the same as the per-call path.
9. **CSV**: plain field values without a Task; **HTML**: `ValueTask` for the per-cell css/action helpers;
   **HTML/RTF**: body written from the builder's chunks (no large-object-heap string); **formatting**: last culture kept.

Also fixed: html text ending in `"><"` made the lexer produce tokens until memory ran out (`PageTextHtmlLexerTests`).

Where the time goes now (WorldFacts PDF, thread-time samples): text measurement in Skia (`SKFont.MeasureText`) about
44%, zlib about 28%, row heights 7%, data 5%. See below for what could move those.

## Remaining opportunities (not done)
- **zlib level** (a size decision): on WorldFacts page content level 5 compresses in 6.5 ms instead of 9.9 ms for
  2.3% larger streams; level 1 in 3.7 ms for 20% larger. 1,000-row table: level 5 is 5.0 vs 5.8 ms for 4.4% larger.
  `ZLibCompressionOptions` (net9+) would allow levels other than Optimal/Fastest.
- **Majorsilence.Forms**: its `main` caches `FontSubstitution.Resolve` (after 26.11.4), which would also cover the other
  `FontFamily` metrics; the engine's own cache stays useful until then. Measuring a run (`MeasureRunWidth`) is now the
  largest single cost of a table-heavy PDF.
- **Data file**: `GetDataFile` reads the XML data into a string before `LoadXml` (about 10% of a CSV render).
- **Async overhead in expression evaluation**: a `Task<object>` / `Task<double>` per non-trivial expression per row
  (`FunctionField.Evaluate`, `Expression.EvaluateAsync`, aggregates). A synchronous path through `IExpr` is the
  general fix; the plain-field cases already have one.
- **Parse**: `XmlDocument` DOM (0.2-1.7 ms per report); only worth it if reports are not cached (`CompiledReport`).

## Allocation gate
`-- gate` (run in `linux.yml` after the tests) renders five workloads and fails if any allocates more than 20% above `allocation-baseline.json`. After an intended change: `dotnet run -c Release -f net10.0 --project Majorsilence.Reporting.Benchmarks -- gate --update` and commit the file. The first CI run should confirm the 20% tolerance is enough for the runner's installed fonts.
