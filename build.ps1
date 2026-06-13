param(
    [switch]$Publish
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$project = Join-Path $root "src/EverythingMcp/EverythingMcp.csproj"

if ($Publish) {
    dotnet publish $project -c Release -r win-x64 --self-contained false -o (Join-Path $root "publish/cli")
    Write-Host "Published to publish/cli"
    exit 0
}

dotnet build $project -c Release
Write-Host "Build complete"
