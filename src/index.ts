#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import {
  formatSearchResponse,
  resolveEsExe,
  searchFiles,
} from "./everything.js";

const searchInputSchema = {
  query: z
    .string()
    .min(1)
    .describe(
      "Everything search query. Supports wildcards (*, ?), operators, and filters like ext:cs path:D:\\source\\repos",
    ),
  max_results: z
    .number()
    .int()
    .min(1)
    .max(1000)
    .optional()
    .describe("Maximum number of results (default 100, max 1000)"),
  scope_path: z
    .string()
    .optional()
    .describe("Limit search to this folder and its subfolders"),
  match_path: z
    .boolean()
    .optional()
    .describe("Match against full path instead of filename only"),
  match_case: z.boolean().optional().describe("Case-sensitive search"),
  files_only: z.boolean().optional().describe("Return files only"),
  folders_only: z.boolean().optional().describe("Return folders only"),
};

const server = new McpServer({
  name: "everything-mcp",
  version: "0.1.0",
});

server.tool(
  "search",
  "Search files and folders across the system using voidtools Everything index. Ideal for cross-project path discovery.",
  searchInputSchema,
  async (args) => {
    const result = searchFiles({
      query: args.query,
      maxResults: args.max_results,
      scopePath: args.scope_path,
      matchPath: args.match_path,
      matchCase: args.match_case,
      filesOnly: args.files_only,
      foldersOnly: args.folders_only,
    });

    return {
      content: [
        {
          type: "text",
          text: formatSearchResponse(result),
        },
      ],
    };
  },
);

server.tool(
  "status",
  "Check Everything CLI availability and resolved es.exe path",
  {},
  async () => {
    const esPath = resolveEsExe();
    const version = spawnEsVersion(esPath);

    return {
      content: [
        {
          type: "text",
          text: [
            "everything-mcp is ready.",
            `es.exe: ${esPath}`,
            version ? `es version: ${version}` : "es version: unavailable",
          ].join("\n"),
        },
      ],
    };
  },
);

function spawnEsVersion(esPath: string): string | undefined {
  try {
    const result = spawnSync(esPath, ["-version"], {
      encoding: "utf8",
      windowsHide: true,
    });
    return (result.stdout ?? result.stderr ?? "").trim() || undefined;
  } catch {
    return undefined;
  }
}

async function main(): Promise<void> {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch((error: unknown) => {
  const message = error instanceof Error ? error.message : String(error);
  console.error(`everything-mcp: ${message}`);
  process.exit(1);
});
