# Async audit: removing sync-over-async, and what .NET 11 changes

Blocking on async work (`Task.Run(...).GetAwaiter().GetResult()`, `.Result`, `.Wait()`) costs a thread-pool hop plus a blocked pool thread per call, can deadlock under a synchronization context, and starves the pool when many reports render at once (a web server). Before this work the engine and data providers had **22 such sites**; the heaviest were inside sort comparers, so a sort of n rows made O(n log n) hops.

## Done (all with identical output; new tests pin the behaviour and also pass on the previous engine)
| Where | Was | Now |
|---|---|---|
| `Rows` (table/list/Top N filter sort) | `Compare` blocked per comparison, twice per sort key | `SortAsync`: each row's keys evaluated once, awaited, then the same `Array.Sort` and row-number tie-break |
| `GroupEntryCompare` (table groups) | blocked per comparison | `GroupEntrySort.SortAsync`, same approach (sets each entry as its group's current group while evaluating) |
| `RecursiveCompare` (recursive groups) | `List.BinarySearch` + blocking comparer | async binary search with `List.BinarySearch`'s exact probing |
| `MatrixEntryComparer` | blocked per comparison | `MatrixEntrySort.SortAsync`; `Matrix.RunBuildSet` awaits `PrepareSortedDataAsync` once after building, so the many synchronous matrix methods only read sorted lists |
| `ChartBase` constructor | blocked on the tooltip expressions | `InitAsync()` awaited from `Chart.RunChartBuild` |
| `DynamicExpression` constructor | blocked on parsing | `CreateAsync` factory |
| `JsonDataReader`, `TxtDataReader`, `MultipleStreamReader` | wrapped async stream opening / `ReadLineAsync` in `Task.Run(...).GetResult()` | synchronous I/O where the contract is synchronous (`IDataReader`, `TextReader.ReadLine`): files were already sync streams, HTTP uses `HttpClient.Send` / `ReadAsStream` |

Measured effect: WorldFacts 50 -> 44 ms and 9.5 -> 8.0 MB per render; thread-pool work items per WorldFacts render fell from ~3,700 to ~15 (0 for the tiny and matrix reports); `-- profile` prints the count.

## Remaining (not done)
| Where | What it needs | Public API? |
|---|---|---|
| `ReportParameter.cs` (5 sites: `DefaultValue`, `DisplayValues`, `DataValues`, `RuntimeValue`, `RuntimeValues` on the report-parameter wrapper) | async methods (`GetDefaultValueAsync()` ...) next to the properties; properties kept as a documented blocking compatibility path. ~50 call sites in the viewer, designer and ASP projects to move over | **Yes** (additive). Decision needed |
| `Report.ClientLanguage` getter (`Report.cs:733`) | evaluate the language expression once, awaited, when the run starts (it has no row), then the getter returns the stored value | Internal; 28 references |
| `ReportItem.DataElementOutput` getter (`ReportItem.cs:895`) | resolve once in `FinalPass` (awaited) and store | Internal |
| `MhtWebFile` download (`MhtWebFile.cs:514`) | MHT/web-archive export only; make the download path async or use `HttpClient.Send` | Internal |
| UI only: `RdlDesign/MDIChild.cs:182` (`FileSaveAs`), `RdlDesign/DesignCtl.cs:1015` (`GetStyleInfo`) | event-handler/command plumbing; belongs with a designer pass, not the engine | Internal |

`RdlViewer` already replaced its blocking calls (the remaining hits are comments).

## Findings along the way (existing behaviour, not changed)
- Matrix rows sorted on an aggregate such as `Sum(...)` keep first-met order: the sort expression is evaluated against the entry's first row. Plain sort expressions work. Pinned in `AsyncSortTests`.
- `MultipleStreamReader`'s path-list parser mishandles quoted paths (the separator after a closing quote starts a new entry, and the last quoted file is queued twice).
- `Textbox.HTMLEXPR` is greedy (`<expr>.+</expr>`), so two `<expr>` blocks in one html text are treated as one expression.

## .NET 11: what to know before leaning on it
.NET 11 adds **runtime async**: the JIT, not the C# compiler, turns `async` methods into suspendable code, so the per-method state-machine class and intermediate `Task` objects are no longer needed for eligible methods; locals can stay on the stack until a real suspension. Sources: [Performance Improvements in .NET 11](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-11/), [What's new in .NET 11 runtime](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime). It is still a preview feature behind the compiler switch `runtime-async=on` (the docs say a `net11.0` project no longer needs `EnablePreviewFeatures`; check the current page before relying on either statement), continuations can skip `ExecutionContext` capture when there is nothing to restore, and suspension points are merged to cut code size. Gains are workload dependent; the claim is parity with .NET 10 at worst and fewer allocations for chains of small async methods.

What that means for this engine:
- Our hottest shape is exactly that: `IExpr.Evaluate*` is a deep chain of tiny `async Task<object>` methods (631 declarations), and expression evaluation was 5-9% of allocation per render (`Task<object>`, `Task<string>`, `UnwrapPromise`). Runtime async should remove most of that **without code changes**, once the library has a `net11.0` target and builds with the feature on. Plan: add `net11.0` to `RdlEngine`/`DataProviders` when it ships (November 2026), turn the switch on for that target, and re-run `-- gate` and the benchmarks; the allocation gate will show the effect directly.
- Do not convert the `IExpr` surface to `ValueTask` by hand now: it is ~630 signatures and the runtime does the equivalent. (A hand-written `ValueTask` fast path is still the right tool for the few *non-async-method* cases that complete synchronously and return a cached value.)
- It does nothing for blocking. A `.GetResult()` still blocks a thread, so the work above stands.
- Things that make runtime async less effective, so avoid adding them: storing or passing `Task` objects around (`Task.WhenAll`, caching tasks), `Task.Run` wrappers, and `async void`. Straight-line `await` chains are the case it optimises.
- `ConfigureAwait(false)` matters for UI hosts (WinForms/Majorsilence.Forms/Avalonia viewers resume every await on the UI thread) and not for ASP.NET Core (no synchronization context). Library code that is host-agnostic should use it, enforced with analyzer CA2007; that is a mechanical change worth doing together with the `net11.0` target, and it is independent of this audit.

## Rules going forward
1. No `Task.Run(...).GetResult()` / `.Result` / `.Wait()` in engine or provider code. Where a contract is synchronous (`IDataReader`, `TextReader`, comparers), use a genuinely synchronous implementation (sync I/O, `HttpClient.Send`) or restructure so the async work happens before the synchronous consumer runs (decorate-then-sort, prepare-then-read).
2. Constructors and property getters must not await: use `InitAsync` / `CreateAsync` / `GetXAsync()`.
3. Comparers that need async data are replaced by "evaluate keys once, then sort on the keys".
4. A thread-pool work-item count in the concurrency benchmark (`Completed Work Items`) is the quick regression signal for sync-over-async creeping back.
