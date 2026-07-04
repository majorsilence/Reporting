# WinForms → Majorsilence.Forms migration notes

Playbook and gotchas from **D1** (`Majorsilence.WinformUtils` → `Majorsilence.WinformUtils.Forms`),
the toolchain-proving exercise for the rest of Track D (D2: RdlViewer, D3: RdlReader, D4-D6:
RdlDesign/ReportDesigner/RdlMapFile). Read this before starting each of those.

## Toolchain

- `Majorsilence.Forms` **is published to nuget.org**, currently at `26.0.0`
  (`Majorsilence.Forms`, `.Avalonia`, `.Headless`, `.Uno`, `.Drawing.Common`, `.Telerik` all
  confirmed live). Add package versions to the root `Directory.Packages.props` under a
  `<!-- Majorsilence.Forms migration track (Track D) -->` comment as you need new backend
  packages (only `Majorsilence.Forms`, `Majorsilence.Forms.Avalonia`, and
  `Majorsilence.Forms.Headless` are there so far).
- The migrator lives in the sibling repo: `../Modern.Forms/tools/Majorsilence.Forms.Migrator`.
  Build it once (`dotnet build -c Release`) and invoke the DLL directly:
  ```
  dotnet ../Modern.Forms/tools/Majorsilence.Forms.Migrator/bin/Release/net10.0/majorsilence-migrate.dll \
    <ProjectDir>/<Project>.csproj \
    -o <ProjectDir>.Forms \
    --backend avalonia --package-version 26.0.0 \
    --report <ProjectDir>.Forms/migration-report.md
  ```
  **Always pass `--package-version 26.0.0` explicitly.** Both of the tool's own defaults (`1.0.4`
  in code, `0.3.0` in `--help` text) are stale and don't match what's actually published.
- **Dry-run first** (`--dry-run --diff`) to read the exact diff before committing to a real run.
- **The migrator only writes files it changes.** If a source file needs zero *textual*
  substitutions (see "implicit usings" below for why that undersells it), it's left out of the
  output tree entirely — copy every other file (`.resx`, unrelated `.cs`, etc.) over by hand.
- **When outputting to a separate directory** (`-o`, the parallel-tree strategy this whole track
  uses), the migrator can't safely edit the shared root `Directory.Packages.props` — that file is
  outside the output tree by design. It emits a warning telling you to add the `PackageVersion`
  entries yourself. Do that as part of every migration, not just D1.
- The migrated `.csproj`'s `TargetFrameworks` needs a manual pass after the tool runs: it
  preserves whatever TFMs the original had (just dropping the `-windows` suffix), including
  `net48` if present — but **`Majorsilence.Forms` itself only targets `net8.0;net10.0`**, so any
  `net48` entry left in a migrated project's TFM list will fail to restore. Drop it explicitly.

## The single biggest real gap: implicit global usings

`UseWindowsForms=true` makes the SDK auto-inject a `GlobalUsings.g.cs` with
`global using System.Drawing;` and `global using System.Windows.Forms;` (confirmed by inspecting
`obj/**/GlobalUsings.g.cs` for the original project) **on top of** the standard
`ImplicitUsings=enable` set (`System`, `System.Collections.Generic`, `System.IO`, `System.Linq`,
`System.Threading.Tasks` — those survive migration fine since `ImplicitUsings=enable` isn't
removed).

The migrator's text engine only rewrites **explicitly-qualified** references
(`System.Windows.Forms.Foo` → `Majorsilence.Forms.Foo`). A file that uses `Form`, `Application`,
`Color`, `Point`, etc. **unqualified**, relying entirely on the implicit global using, is
therefore invisible to the tool: no textual match, no rewrite, and — worse — the file won't even
show up as "changed" in the migration report, so it's easy to assume it needed no work at all.
Once `UseWindowsForms` is stripped from the `.csproj`, that implicit using disappears and the file
stops compiling.

**Action for every future migration:** after running the tool, grep the *original* project for
files with no `using System.Windows.Forms;`/`using System.Drawing;` at the top that still use
unqualified WinForms/Drawing types (`Form`, `Control`, `Application`, `Color`, `Point`, `Size`,
etc.). Add `using System.Drawing;` (real BCL primitives — `Color`/`Point`/`Size`/`Rectangle` stay
as-is, they're never rewritten to `Majorsilence.Forms.Drawing`) and `using Majorsilence.Forms;`
(for `Form`, `Application`, and all the control types) to those files by hand. For D1 this was 2
of the project's 4 source files (`WaitForm.cs`, `FormExtensions.cs`) — don't assume "0 warnings
from the tool" means "nothing to check."

Consider trying `--engine roslyn` (symbol-accurate, needs a loadable project) for D4/D5/D6 given
their size — it may not have this blind spot since it resolves symbols rather than matching text.
Not verified in D1; worth a quick trial before starting D4.

## The other big one: `Form` is not a `Control`

This is an actual API design difference in Majorsilence.Forms, not a migrator limitation — no
tool would have caught it, and no future migration on a project that uses `Form` for anything
beyond the most basic "show a window" will avoid running into it.

In classic `System.Windows.Forms`: `Form : ContainerControl : ScrollableControl : Control`. A
`Form` *is* a `Control` — you can pass one anywhere a `Control` or `ContainerControl` is expected,
call `Width`/`Height`/`PointToScreen`/`Refresh()` on it, subscribe to `SizeChanged`/`Move`, etc.

In Majorsilence.Forms: `Form : WindowBase : Component`. `Control`/`ContainerControl`/`Panel`/
`UserControl` are a **separate** hierarchy, also rooted at `Component`. A `Form` and a
`ContainerControl` share no inheritance relationship — code like
`if (control is Form) { ... }` where `control` is statically typed `ContainerControl` won't just
misbehave, it **won't compile** (`CS8121: An expression of type 'ContainerControl' cannot be
handled by a pattern of type 'Form'`).

Concrete consequences hit in D1, and what to reach for instead:

| Classic WinForms (on `Form`) | Majorsilence.Forms equivalent | Notes |
|---|---|---|
| `Width` / `Height` (int) | `Size.Width` / `Size.Height` | `Size` is `get`+`set` on `Form` (a `new` override hiding `WindowBase`'s get-only `Size`) |
| `Location` (settable) | `Location` | Also a settable `new` override on `Form`; screen-relative already, no `PointToScreen` needed |
| `Bounds` (settable) | `Bounds` | Settable on `Form`; the cleanest way to reposition+resize in one call |
| `SizeChanged` / `Move` events | *(none)* | No equivalent events exist. If you need to track a moving/resizing owner, poll `Bounds` on an existing timer tick rather than trying to subscribe to something that isn't there. |
| `HandleCreated` event | `Shown` event, or `IsHandleCreated` property | `Shown` fires once and was the closest semantic match for "safe to start doing work now" in D1; `IsHandleCreated` exists if you need a poll-able check instead of an event |
| `DoubleBuffered` | *(none, and not needed)* | Avalonia-backed rendering doesn't need this WinForms-specific GDI+ hint; just delete the assignment |
| `Refresh()` | `Invalidate()` | `Invalidate()` marks dirty for the next paint; there's no immediate/synchronous repaint call |
| `parent.PointToScreen(...)` where `parent` might be a `Form` | Branch on type first | `PointToScreen` is a `Control` member `Form` doesn't have. If code needs to compute a `Form`-or-`ContainerControl`'s screen bounds generically, use a `Func<Rectangle>` delegate supplied by the caller (who knows which concrete type they have) rather than trying to accept a common base type — there isn't one below `Component`. |
| `IsDisposed` | *(none on `Form`/`WindowBase`)* | No disposed-state property to pre-check; rely on `try { ... } catch (ObjectDisposedException) { }` around whatever call would actually fail, the same as D1's timer callback does. |

**Action for D2+:** grep the target project for `is Form` / `as Form` patterns and for any method
that accepts `ContainerControl` (or `Control`) but is also invoked with a `Form` argument
somewhere. Both are guaranteed compile breaks, not warnings. RdlViewer (D2) is the "crown jewel"
per the plan specifically because of its GDI+ paint/print/zoom code — expect this table to grow
during that item, and expect it to be a bigger lift than D1's few call sites.

## Testing pattern

`Majorsilence.Forms.Headless` (also on nuget.org at `26.0.0`) is a real dependency-free backend —
no windowing toolkit, no UI-thread affinity — built exactly for this. Select it once per test
assembly via a `[ModuleInitializer]`, mirroring `Majorsilence.Forms`'s own test suite:

```csharp
using System.Runtime.CompilerServices;
using Majorsilence.Forms.Backends;
using Majorsilence.Forms.Headless;

internal static class TestBackend
{
    [ModuleInitializer]
    internal static void Initialize() => Platform.Backend = new HeadlessPlatformBackend();
}
```

Then instantiate real `Form`/`Control`-derived types in ordinary NUnit tests, no mocking needed.
D1's `Majorsilence.WinformUtils.Forms.Tests` (5 tests) exercises the actual `ShowWaiter`/
`HideWaiter` extension methods this way, including `Application.OpenForms` count assertions —
genuine behavior verification, not just "it constructs without throwing."

## Package/versioning conventions used (carry forward)

- Package ID gets a `.Forms` suffix: `Majorsilence.WinformUtils.Forms` (folder matches: new
  parallel directory `Majorsilence.WinformUtils.Forms/`, original `Majorsilence.WinformUtils/`
  untouched).
- `<VersionSuffix>preview</VersionSuffix>` until D8's parity sign-off flips it.
- `Description` explicitly says what it's a preview successor to and why (cross-platform vs.
  Windows-only), so anyone browsing NuGet understands the relationship without reading this file.
