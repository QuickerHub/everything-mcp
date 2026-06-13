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
| `status` | Check bundled SDK DLL and Everything client state |

### `search` parameters

| Parameter | Description |
|-----------|-------------|
| `query` | Everything query (`ext:cs`, wildcards, etc.) |
| `max_results` | Default 100, max 1000 |
| `scope_path` | Limit to folder subtree |
| `match_path` | Match full path |
| `match_case` | Case-sensitive |
| `match_whole_word` | Whole words only |
| `regex` | Regex mode |
| `sort_by` | `name_asc`, `date_modified_desc`, ... |
| `auto_start` | Try starting Everything tray client (default true) |

### Example

```json
{
  "query": "quicker-rpc",
  "scope_path": "D:\\source\\repos\\quicker",
  "max_results": 20
}
```

## Development

```powershell
.\build.ps1          # build only
.\build.ps1 -Publish # publish to publish/cli
.\publish\cli\everything-mcp.exe --smoke-test
```

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
