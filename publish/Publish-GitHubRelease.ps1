param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$OutputDir = "./artifacts"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $root
Set-Location $repoRoot

$publishDir = Join-Path $repoRoot "publish/cli"
$artifactDir = Join-Path $repoRoot $OutputDir
$zipName = "everything-mcp-win-x64-v$Version.zip"
$zipPath = Join-Path $artifactDir $zipName

if (Test-Path $artifactDir) {
    Remove-Item $artifactDir -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null

& (Join-Path $repoRoot "build.ps1") -Publish

if (-not (Test-Path (Join-Path $publishDir "everything-mcp.exe"))) {
    throw "Publish output missing everything-mcp.exe"
}

& (Join-Path $publishDir "everything-mcp.exe") --smoke-test-offline | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Offline smoke test failed"
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Host "Created $zipPath"

$hash = Get-FileHash $zipPath -Algorithm SHA256
$hashPath = "$zipPath.sha256"
Set-Content -Path $hashPath -Value "$($hash.Hash)  $zipName" -NoNewline
Write-Host "Created $hashPath"
