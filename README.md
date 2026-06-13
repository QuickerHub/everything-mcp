# @quickerhub/everything-mcp

Fast cross-project file search for AI agents via [voidtools Everything](https://www.voidtools.com/) on Windows.

Published by [QuickerHub](https://github.com/QuickerHub). Use with Cursor, Claude Desktop, VS Code Copilot, Windsurf, and other MCP hosts.

## Why

Cursor and other agents only index the **current workspace**. When you work across many repos under `D:\source\repos`, built-in Glob/SemanticSearch often cannot find directories in other projects.

`everything-mcp` exposes Everything's indexed search to agents so they can locate files and folders in milliseconds across the whole machine.

## Prerequisites

1. **Windows**
2. [Everything](https://www.voidtools.com/) installed and **running** (tray icon visible)
3. Everything CLI `es.exe` available (ships with Everything; usually in the install folder)
4. Node.js 18+

Optional: set `EVERYTHING_ES_PATH` if `es.exe` is not in a default location.

## MCP config

### Cursor / Claude Desktop (`mcpServers`)

```json
{
  "mcpServers": {
    "everything-search": {
      "command": "npx",
      "args": ["-y", "@quickerhub/everything-mcp"]
    }
  }
}
```

### VS Code Copilot (`servers`)

```json
{
  "servers": {
    "everything-search": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@quickerhub/everything-mcp"]
    }
  }
}
```

### Global install

```powershell
npm install -g @quickerhub/everything-mcp
```

```json
{
  "command": "everything-mcp",
  "args": []
}
```

## Tools

| Tool | Description |
|------|-------------|
| `search` | Search files/folders via Everything index |
| `status` | Check `es.exe` resolution and CLI availability |

### `search` parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `query` | string | Everything query (wildcards, `ext:`, `path:`, etc.) |
| `max_results` | number | Default 100, max 1000 |
| `scope_path` | string | Limit to folder subtree |
| `match_path` | boolean | Match full path |
| `match_case` | boolean | Case-sensitive |
| `files_only` | boolean | Files only |
| `folders_only` | boolean | Folders only |

### Example queries

```
quicker-rpc
ext:cs path:D:\source\repos\quicker
*.slnx
CeaQuickerTools QuickerUtil
```

## Development

```powershell
cd D:\source\repos\quicker\everything-mcp
npm install
npm run build
npm start
```

Debug with MCP Inspector:

```powershell
npx @modelcontextprotocol/inspector node dist/index.js
```

## Publish

npm package releases are triggered by GitHub Release tags (`v*`) via `.github/workflows/publish.yml`.

```powershell
git tag v0.1.0
git push origin v0.1.0
```

Requires `NPM_TOKEN` secret on the `QuickerHub/everything-mcp` repository.

## Related

- [QuickerHub/quicker-rpc](https://github.com/QuickerHub/quicker-rpc) — Quicker action authoring MCP (`@quickerhub/qkrpc-mcp`)
- [voidtools Everything](https://www.voidtools.com/) — indexed file search engine

## License

MIT
