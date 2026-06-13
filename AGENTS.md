# everything-mcp (Agent notes)

MCP server: `@quickerhub/everything-mcp` — Windows file search via Everything `es.exe`.

## When to use

- User asks to find a file/folder **outside the current workspace**
- Cross-project paths under `D:\source\repos\...`
- Fast filename/path lookup (not semantic code search)

## When not to use

- Code semantics inside current repo → use SemanticSearch / Grep
- Non-Windows platforms → not supported yet

## Setup check

Call `status` first. If it fails, tell the user to install and run Everything.

## Common scopes

```
scope_path: D:\source\repos\quicker
scope_path: D:\source\repos\clip
query: quicker-rpc
query: ext:cs QuickerUtil
```

## Repo

https://github.com/QuickerHub/everything-mcp
