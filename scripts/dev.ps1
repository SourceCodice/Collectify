$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$backendProject = Join-Path $root "src\backend\Collectify.Api\Collectify.Api.csproj"
$frontendRoot = Join-Path $root "src\frontend\collectify-desktop"
$nodeModules = Join-Path $frontendRoot "node_modules"

if (-not (Test-Path $nodeModules)) {
    Write-Host "Dipendenze frontend mancanti."
    Write-Host "Esegui prima:"
    Write-Host "  cd $frontendRoot"
    Write-Host "  npm.cmd install"
    exit 1
}

Write-Host "Collectify avviato."
Write-Host "API: http://localhost:5088"
Write-Host "Renderer: http://127.0.0.1:5173"

dotnet run --project "$backendProject" --launch-profile Collectify.Api
