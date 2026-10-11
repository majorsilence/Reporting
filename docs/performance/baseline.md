# RdlEngine performance baseline

Taken on `main` after PR #435. net10.0, BenchmarkDotNet 0.15.8 `--job short` (3 iterations; use for ranking, allocated bytes are exact, timings have wide error).
Machine: Intel Core i5-8350U (4 cores / 8 threads), Ubuntu 26.04, .NET 10.0.12, workstation concurrent GC, TieredPGO default.
Raw BDN tables: `raw/*-net10.md`. Reproduce:

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
