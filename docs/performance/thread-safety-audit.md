# Thread-safety audit (static / shared state)

Scope: RdlEngine, Majorsilence.Pdf, DataProviders, RdlCri. Contract: one `Report` / `Graphics` per thread; everything else shared must be safe.
Source: code reading only (not yet confirmed with a failing test). Items marked **verify** need a repro before fixing.
`Majorsilence.Forms.Drawing.Common` (NuGet) statics were not audited.

## Unsafe / contention (fix or guard, highest risk first)

| Risk | Location | Problem | Planned handling |
|---|---|---|---|
| High | `RdlEngine/Runtime/RdlEngineConfig.cs` 24-31, 97-139, 410-412 | `RdlEngineConfigInit` writes 5 static fields unsynchronised; `SqlEntries` published before `CustomReportItemEntries` (possible NRE); racy early-return; check-then-act on `SqlEntries == null` | Single lock + one immutable snapshot published once. Also check whether `RdlAsp.Mvc/RdlReport.cs:255` calls Init per request. Concurrency test: N threads, first call. |
| High | `Render/MhtConverter/MhtWebFile.cs` 1090-1130 | Broken double-checked locking: `htmlEntitiesTable` assigned before populated | Build in a local, publish after (or `Lazy<T>`). MHT only. |
| Medium | `RdlEngineConfig.cs` 35-37, 68-69, 839-915 | Plain `Dictionary` fields lazily created / written by `Register*`; read while rendering | Document "register at startup"; make registration copy-on-write or lock. |
| Medium | `DataProviders/MultipleStreamReader.cs` 19, 114 | Shared static `HttpClient` headers mutated per fetch (check-then-act on `UserAgent`) | Configure once in static initialiser (as `Image.cs:31-37` does). |
| Low | `Definition/Rdl2008Normalizer.cs` 1220, `Rdl2008GaugeConverter.cs` 111 | `static int ++` counters: duplicate generated names, non-deterministic output | `Interlocked.Increment`. |
| Low | `Runtime/ImageQualityManager.cs`, `ExcelCellsBuilder.cs:17`, `Rdl2008GaugeConverter.cs:182` | Mutable process-global config | Document as startup-only. |
| Contention | `Definition/ChartMapData.cs` 16, 231, 343-356 | Global lock held across file stat + XML parse on every map chart; cached `MapData` has a mutable dictionary (**verify** whether renders mutate it) | Check timestamp outside the lock; make cached data immutable. |
| Contention | `Render/StreamGen.cs:106` | `lock (typeof(StreamGen))` around file creation (file output only) | Private lock object; low priority. |
| Perf | `Majorsilence.Pdf/InstalledFontScanner.cs` 48-53 | `GetOrAdd` factory not locked: N concurrent cold requests run N font-folder scans | `Lazy<T>` values; warm at startup. |
| Perf | `Render/RenderPdf_Raw.cs` 440-500, `Majorsilence.Pdf/Internal/FontCache.cs`, `PdfDocument.cs:35` | New `FontRegistry` per report, re-enumerates font folders; font file read + TTF parse per report per font | Shared immutable parsed-font cache (thread-safe by construction). Likely a top web-load win. |
| Limited | `DataProviders/WebServiceWsdl.cs` 28-40, 107 | check-then-act can throw duplicate-key (WSDL provider, unsupported on modern .NET) | Ignore unless used. |

## Already safe (keep safe when optimising)
- `RdlEngineConfig._precompiledCode` (`ConcurrentDictionary`); `Report._ExternalImages` / `_RunCache` (per report).
- `PageTextHtml.cs` `ConditionalWeakTable<Graphics, MeasureCache>` with per-Graphics `lock(this)` (PR #435): no cross-request contention, but only correct while a `Graphics` is not shared between threads.
- `Interlocked` counters (`Parser.cs:46`, `MemoryStreamGen.cs:21`); static lookup tables / `HashSet`s / width tables (read-only after init); `Image.cs` static `HttpClient`.
- No `[ThreadStatic]`, `AsyncLocal`, culture mutation, or `SetCurrentDirectory` in the audited trees.

## Rules for new caches
Per-report/per-render caches need no locks. Process-wide caches: `ConcurrentDictionary` with side-effect-free factories, `Lazy<T>` (`ExecutionAndPublication`) for expensive factories, or immutable snapshots. Never share Skia/`Graphics`/`Bitmap` objects across threads.
