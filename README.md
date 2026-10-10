# @quickerhub/everything-mcp

Fast cross-project file search for AI agents via [voidtools Everything](https://www.voidtools.com/) on Windows.

Uses the **Everything SDK DLL** (same approach as QuickerPc), not `es.exe`.

Published by [QuickerHub](https://github.com/QuickerHub).

## Architecture

```
Cursor / Claude / VS Code
  → everything-mcp.exe (MCP stdio)
  → Everything64.dll (bundled SDK IPC client)
  → Everything.exe (user-installed tray client + index engine)
```

The bundled DLL is only an IPC client. **Everything.exe must be installed and running** to maintain the file index.

## Prerequisites

1. **Windows x64**
2. [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
3. [Everything](https://www.voidtools.com/) 1.4.x installed (stable). Tray client should be running.
4. Node.js 18+ (only for `npx` launcher)

No `es.exe` / ES CLI required.

## MCP config

### Direct exe (recommended for local dev)

```json
{
  "mcpServers": {
    "everything-search": {
      "command": "D:\\source\\repos\\quicker\\everything-mcp\\publish\\cli\\everything-mcp.exe",
      "args": []
    }
  }
}
```

Build first:

```powershell
cd D:\source\repos\quicker\everything-mcp
.\build.ps1 -Publish
```

### npm launcher

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

Override exe path:

```json
{
  "env": {
    "EVERYTHING_MCP_EXE": "D:\\path\\to\\everything-mcp.exe"
  }
}
```

## Tools

| Tool | Description |
|------|-------------|
| `search` | Search files/folders via Everything index (returns JSON) |
| `status` | Check SDK DLL and Everything state; returns `install_guide` when not ready |
| `install_guide` | Structured winget/manual install steps for Everything.exe |

### `search` parameters (agent-first)

Prefer structured params over raw Everything `query`.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `name` | — | Filename (`QuickerUtil.cs`, `*.cs`) |
| `extension` | — | Extension without dot (`cs`, `md`) |
| `folder` | — | Search under this directory tree |
| `path_contains` | — | Path must contain this text |
| `kind` | `file` | `file`, `folder`, or `any` |
| `query` | — | Advanced Everything syntax (optional) |
| `scope_path` | — | Deprecated alias for `folder` |
| `max_results` | 25 | Max 100 after filtering |
| `apply_default_filters` | true | Skip `node_modules`, `.git`, caches, etc. |
| `include_build_artifacts` | false | Include `bin`, `obj`, `dist`, `build`, `target` |
| `include_hidden` | false | Include `.vs`, `.cache`, etc. |
| `exclude` | — | Extra path segment names to skip |
| `sort_by` | `date_modified_desc` | Sort order |
| `auto_start` | true | Start Everything tray if needed |

### Examples

```json
{ "name": "QuickerUtil.cs", "folder": "D:\\source\\repos" }
```

```json
{ "extension": "cs", "folder": "D:\\source\\repos\\clip\\CeaQuickerTools", "max_results": 10 }
```

```json
{ "name": "quicker-rpc", "kind": "folder", "folder": "D:\\source\\repos\\quicker" }
```

## Development

```powershell
.\build.ps1          # build only
.\build.ps1 -Publish # publish to publish/cli
.\publish\cli\everything-mcp.exe --smoke-test
```

## Release

**Latest:** https://github.com/QuickerHub/everything-mcp/releases/latest

```powershell
.\publish\Publish-GitHubRelease.ps1 -Version 0.2.0
gh release create v0.2.0 --title "v0.2.0" artifacts/*
```

### npm (`@quickerhub/everything-mcp`)

Requires `NPM_TOKEN` repo secret (npm automation token with publish access to `@quickerhub`).

```powershell
gh secret set NPM_TOKEN --repo QuickerHub/everything-mcp
gh workflow run publish.yml --repo QuickerHub/everything-mcp -f version=0.2.0
```

Creating a GitHub Release also triggers the npm publish workflow automatically.

## Comparison with QuickerPc

| | QuickerPc | everything-mcp |
|--|-----------|----------------|
| SDK DLL | `Everything64.dll` bundled | same |
| Everything.exe | user-installed, required | same |
| IPC | P/Invoke | P/Invoke |
| es.exe | not used | not used |

## Related

- [QuickerHub/quicker-rpc](https://github.com/QuickerHub/quicker-rpc)
- [voidtools SDK](https://www.voidtools.com/support/everything/sdk/)

## License

MIT. Everything SDK DLL is from voidtools (public SDK); not affiliated with voidtools.
