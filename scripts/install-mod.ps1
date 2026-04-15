param(
    [string]$GameRoot = "D:\Users\hosgoo\Downloads\Slay the Spire 2\Slay the Spire 2",
    [string]$ModName = "STS2-Link-YOKONEX"
)

$ErrorActionPreference = "Stop"

$sourceDir = Join-Path $PSScriptRoot "..\artifacts\mods\$ModName"
$sourceDir = [System.IO.Path]::GetFullPath($sourceDir)
$modsDir = Join-Path $GameRoot "mods"
$targetDir = Join-Path $modsDir $ModName

if (-not (Test-Path $sourceDir)) {
    throw "Mod package not found: $sourceDir"
}

New-Item -ItemType Directory -Force -Path $modsDir | Out-Null
if (Test-Path $targetDir) {
    Remove-Item -LiteralPath $targetDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

Copy-Item -LiteralPath (Join-Path $sourceDir "$ModName.dll") -Destination $targetDir -Force
Copy-Item -LiteralPath (Join-Path $sourceDir "$ModName.json") -Destination $targetDir -Force

Write-Host "Installed $ModName to $targetDir"
