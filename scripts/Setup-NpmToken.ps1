param(
    [string]$Repo = "QuickerHub/everything-mcp"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking npm login..."
try {
    $user = npm whoami 2>$null
    if (-not $user) { throw "not logged in" }
    Write-Host "Logged in as: $user"
}
catch {
    Write-Host "Not logged in. Run this first in your terminal:" -ForegroundColor Yellow
    Write-Host "  npm login --auth-type=web"
    Write-Host "Then re-run: .\scripts\Setup-NpmToken.ps1"
    exit 1
}

Write-Host "Creating publish token for GitHub Actions..."
$raw = npm token create --read-only=false 2>&1
Write-Host $raw

# npm prints token like: npm_xxxxxxxx
$token = ($raw | Select-String -Pattern 'npm_[A-Za-z0-9]+' -AllMatches).Matches.Value | Select-Object -Last 1
if (-not $token) {
    throw "Failed to parse token from npm output. Create one manually at https://www.npmjs.com/settings/tokens"
}

Write-Host "Setting GitHub secret NPM_TOKEN on $Repo ..."
$token | gh secret set NPM_TOKEN --repo $Repo

Write-Host "Done. Trigger publish with:" -ForegroundColor Green
Write-Host "  gh workflow run publish.yml --repo $Repo -f version=0.2.0"
