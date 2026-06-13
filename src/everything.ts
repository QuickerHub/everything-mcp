import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

export interface SearchOptions {
  query: string;
  maxResults?: number;
  scopePath?: string;
  matchPath?: boolean;
  matchCase?: boolean;
  filesOnly?: boolean;
  foldersOnly?: boolean;
}

export interface SearchResult {
  paths: string[];
  count: number;
  esPath: string;
}

const COMMON_ES_PATHS = [
  process.env.EVERYTHING_ES_PATH,
  process.env.ProgramFiles
    ? path.join(process.env.ProgramFiles, "Everything", "es.exe")
    : undefined,
  process.env["ProgramFiles(x86)"]
    ? path.join(process.env["ProgramFiles(x86)"], "Everything", "es.exe")
    : undefined,
  process.env.LOCALAPPDATA
    ? path.join(process.env.LOCALAPPDATA, "Everything", "es.exe")
    : undefined,
].filter((value): value is string => Boolean(value));

function resolveEsFromPath(): string | undefined {
  const result = spawnSync("where", ["es"], {
    encoding: "utf8",
    windowsHide: true,
    shell: true,
  });

  if (result.status !== 0) {
    return undefined;
  }

  const first = (result.stdout ?? "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .find(Boolean);

  if (first && fs.existsSync(first)) {
    return first;
  }

  return undefined;
}

export function resolveEsExe(): string {
  for (const candidate of COMMON_ES_PATHS) {
    if (fs.existsSync(candidate)) {
      return candidate;
    }
  }

  const fromPath = resolveEsFromPath();
  if (fromPath) {
    return fromPath;
  }

  throw new Error(
    [
      "es.exe not found. Install Everything from https://www.voidtools.com/",
      "or set EVERYTHING_ES_PATH to the full path of es.exe.",
    ].join(" "),
  );
}

function buildSearchQuery(options: SearchOptions): string {
  const maxResults = clampMaxResults(options.maxResults);
  const trimmed = options.query.trim();
  if (!trimmed) {
    throw new Error("query must not be empty");
  }

  return `count:${maxResults} ${trimmed}`;
}

function buildEsArgs(esPath: string, options: SearchOptions): string[] {
  const maxResults = clampMaxResults(options.maxResults);
  const args = ["-n", String(maxResults)];

  if (options.matchCase) {
    args.push("-case");
  }
  if (options.matchPath) {
    args.push("-match-path");
  }
  if (options.filesOnly) {
    args.push("/a-d");
  }
  if (options.foldersOnly) {
    args.push("/ad");
  }
  if (options.scopePath?.trim()) {
    args.push("-path", options.scopePath.trim());
  }

  args.push(buildSearchQuery(options));
  return args;
}

function clampMaxResults(value: number | undefined): number {
  const maxResults = value ?? 100;
  if (!Number.isFinite(maxResults) || maxResults < 1) {
    return 1;
  }
  return Math.min(Math.floor(maxResults), 1000);
}

export function searchFiles(options: SearchOptions): SearchResult {
  if (process.platform !== "win32") {
    throw new Error("everything-mcp currently supports Windows only");
  }
  if (options.filesOnly && options.foldersOnly) {
    throw new Error("files_only and folders_only cannot both be true");
  }

  const esPath = resolveEsExe();
  const args = buildEsArgs(esPath, options);
  const result = spawnSync(esPath, args, {
    encoding: "utf8",
    windowsHide: true,
    maxBuffer: 16 * 1024 * 1024,
  });

  if (result.error) {
    throw new Error(`failed to run es.exe: ${result.error.message}`);
  }

  const stderr = (result.stderr ?? "").trim();
  if (result.status !== 0) {
    const detail = stderr || `es.exe exited with code ${result.status ?? "unknown"}`;
    if (/ipc|everything/i.test(detail)) {
      throw new Error(
        `${detail}. Make sure Everything is installed and running.`,
      );
    }
    throw new Error(detail);
  }

  const paths = (result.stdout ?? "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  return {
    paths,
    count: paths.length,
    esPath,
  };
}

export function formatSearchResponse(result: SearchResult): string {
  if (result.count === 0) {
    return "No results found.";
  }

  const lines = result.paths.map((filePath, index) => `${index + 1}. ${filePath}`);
  return [`Found ${result.count} result(s):`, ...lines].join("\n");
}
