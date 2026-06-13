#!/usr/bin/env node
/**
 * Launches the .NET everything-mcp stdio server.
 * Requires a published build at publish/cli/everything-mcp.exe (run build.ps1 -Publish).
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");

function resolveExe() {
  if (process.env.EVERYTHING_MCP_EXE?.trim()) {
    const explicit = process.env.EVERYTHING_MCP_EXE.trim();
    if (fs.existsSync(explicit)) {
      return explicit;
    }
  }

  const published = path.join(root, "publish", "cli", "everything-mcp.exe");
  if (fs.existsSync(published)) {
    return published;
  }

  return null;
}

const exe = resolveExe();
if (!exe) {
  console.error(
    "everything-mcp: publish/cli/everything-mcp.exe not found. Run .\\build.ps1 -Publish or set EVERYTHING_MCP_EXE.",
  );
  process.exit(1);
}

const child = spawn(exe, process.argv.slice(2), {
  stdio: "inherit",
  env: process.env,
  cwd: path.dirname(exe),
});

child.on("error", (err) => {
  console.error(`everything-mcp: failed to start ${exe}`);
  console.error(err.message);
  process.exit(1);
});

child.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 1);
});
