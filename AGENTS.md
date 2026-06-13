# everything-mcp (Agent notes)

.NET MCP server using bundled `Everything64.dll` (voidtools SDK IPC client).

## Requires

- Windows + .NET 8 runtime
- Everything.exe installed and running (tray). `auto_start: true` on `search` can launch it.

## Does NOT use

- `es.exe` CLI
- Node search implementation (removed in v0.2)

## Tools

- `search` → JSON with `path`, `name`, `size`, `modified`, `is_folder`
- `status` → SDK DLL path + Everything install/running state

## Cross-project scopes

```
scope_path: D:\source\repos\quicker
scope_path: D:\source\repos\clip
query: quicker-rpc
query: ext:cs QuickerUtil
```

## Local exe

`publish/cli/everything-mcp.exe` after `.\build.ps1 -Publish`
