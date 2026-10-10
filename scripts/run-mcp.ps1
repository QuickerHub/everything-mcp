param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$ErrorActionPreference = "Stop"

function Resolve-EverythingMcpExe {
    if ($env:EVERYTHING_MCP_EXE -and (Test-Path $env:EVERYTHING_MCP_EXE)) {
        return (Resolve-Path $env:EVERYTHING_MCP_EXE).Path
    }

    $here = Split-Path -Parent $MyInvocation.MyCommand.Path
    $published = Join-Path (Split-Path $here -Parent) "publish\cli\everything-mcp.exe"
    if (Test-Path $published) {
        return (Resolve-Path $published).Path
    }

    throw "everything-mcp.exe not found. Run .\build.ps1 -Publish or set EVERYTHING_MCP_EXE."
}

$exe = Resolve-EverythingMcpExe

try {
    $pingJson = & $exe --ping 2>$null | Out-String
    $ping = $pingJson | ConvertFrom-Json
    if (-not $ping.ok) {
        [Console]::Error.WriteLine("everything-mcp preflight: Everything not fully ready. MCP may still start; use --search CLI fallback if tools fail.")
    }
}
catch {
    [Console]::Error.WriteLine("everything-mcp preflight warning: $($_.Exception.Message)")
}

& $exe @Args
