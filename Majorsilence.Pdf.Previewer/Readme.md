# Majorsilence.Pdf.Previewer

Live PDF preview while you code. A `dotnet` global tool that watches a PDF file and reloads a browser tab whenever it changes on disk — the same "edit code, see the result" loop as `dotnet watch`, but for the PDF your code produces.

This is a dev-time tool, not a runtime dependency. Nothing in `Majorsilence.Pdf` or `Majorsilence.Pdf.Markdown` requires it.

## Install

```bash
dotnet tool install --global Majorsilence.Pdf.Previewer
```

## Usage

### Watch an existing PDF

```bash
mspdf-preview watch ./bin/Debug/net10.0/invoice.pdf
```

Open the printed URL (`http://localhost:5990` by default) in a browser. Rebuild your program however you normally would; as soon as the PDF file changes, the tab reloads automatically.

### Run your project and watch its output

```bash
mspdf-preview run . --pdf bin/Debug/net10.0/invoice.pdf
```

This starts `dotnet watch run` in the given project directory (so your program rebuilds and re-renders on every source change) while watching the `--pdf` path and reloading the browser on every change. Pass extra arguments to your program after `--`:

```bash
mspdf-preview run . --pdf bin/Debug/net10.0/invoice.pdf -- --customer "Acme Corp"
```

Both commands accept `--port <number>` to use a port other than 5990.

## How it works

- A small Kestrel server exposes three routes: `/` (the viewer page, using the browser's native PDF renderer via `<embed>`), `/pdf` (the current file bytes), and `/events` (a server-sent-events stream that pushes a `reload` message whenever the watched file changes).
- File changes are detected with `FileSystemWatcher` and debounced (250ms) so a single build that writes the file more than once only triggers one reload.
- Reads retry briefly if the file is transiently locked by whatever process is still writing it.

There is no PDF→PNG rendering here — the browser renders the PDF itself, natively. PNG thumbnails or a page-strip view are explicitly out of scope for this version; if ever added, they'd use a PDF rasterizer inside this tool only, never inside `Majorsilence.Pdf` itself.

## Verifying the auto-reload behavior manually

The SSE reload mechanism is exercised by hand rather than by an HTTP-level automated test (see `Majorsilence.Pdf.Previewer.Tests` for why — nested `dotnet`-process streaming HTTP calls from inside the test host proved unreliable in CI even though the same request succeeds instantly from a shell). To confirm it yourself:

```bash
# Terminal 1
mspdf-preview watch ./some.pdf --port 5990

# Terminal 2 — open the SSE stream and leave it running
curl -N http://127.0.0.1:5990/events

# Terminal 3 — touch the file a few times in quick succession
for i in 1 2 3 4 5; do echo x >> ./some.pdf; sleep 0.02; done
```

Terminal 2 should print exactly one `data: reload` line per burst of writes, not five.
