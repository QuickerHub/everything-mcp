# everything-mcp (Agent notes)

.NET MCP server using bundled `Everything64.dll` (voidtools SDK IPC client).

## MCP vs CLI (read this first)

| Situation | What to do |
|-----------|------------|
| MCP `everything-search` connected | Use MCP tools `search`, `status` |
| MCP returns **Not connected** | **Do not retry MCP.** Run CLI via shell (below) |
| First-time setup | `everything-mcp.exe agent setup` then ask user to reload MCP once |
| Health check | `everything-mcp.exe --ping` |

**Agents cannot restart Cursor MCP.** CLI is the reliable fallback.

### CLI fallback (same behavior as MCP tools)

```powershell
everything-mcp.exe --search --name QuickerUtil.cs --folder D:\source\repos
everything-mcp.exe --search --name CeaQuickerTools --folder D:\source\repos --kind any
everything-mcp.exe --status
everything-mcp.exe --ping
everything-mcp.exe agent setup --check
```

Resolve exe: `publish/cli/everything-mcp.exe`, npm `@quickerhub/everything-mcp`, or `$env:EVERYTHING_MCP_EXE`.

## Requires

- Windows + .NET 8 runtime
- Everything.exe installed and running (tray). `auto_start: true` on `search` can launch it.

## Agent-first `search` API

**Do not learn Everything syntax by default.** Use structured params:

| Param | Example | Purpose |
|-------|---------|---------|
| `name` | `QuickerUtil.cs`, `*.cs` | Filename match |
| `extension` | `cs`, `md` | File type |
| `folder` | `D:\source\repos` | Scope root |
| `path_contains` | `quicker-rpc` | Path substring |
| `kind` | `file` (default), `folder`, `any` | Result type |
| `query` | `ext:cs dm:last7days` | Advanced escape hatch only |

Defaults tuned for agents:
- `max_results`: 25
- `kind`: `file`
- `apply_default_filters`: true (skips `node_modules`, `.git`, build outputs, etc.)
- `include_build_artifacts`: false
- `include_hidden`: false

## Tools

- `search` → agent-friendly file/folder discovery
- `status` → SDK DLL + Everything state; includes `install_guide` + `cli_fallback`
- `install_guide` → winget/manual install steps for Everything.exe

## Local exe

`publish/cli/everything-mcp.exe` after `.\build.ps1 -Publish`

## MCP config (recommended)

Direct exe path (more reliable than npx on Windows):

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

Or use preflight launcher: `powershell -File scripts/run-mcp.ps1`

Install via: `everything-mcp.exe agent setup`
