// Runs the published browser-wasm app under Node and forwards its exit code.
// usage: node run-node.mjs [publish-dir]   (default: bin/Release/net10.0/publish/wwwroot)
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";

const dir = path.resolve(process.argv[2] ?? path.join(path.dirname(fileURLToPath(import.meta.url)), "bin/Release/net10.0/publish/wwwroot"));
const { dotnet } = await import(pathToFileURL(path.join(dir, "_framework/dotnet.js")).href);
const runtime = await dotnet.withDiagnosticTracing(false).withApplicationArguments(...process.argv.slice(3)).create();
const code = await runtime.runMain();
process.exit(code);
