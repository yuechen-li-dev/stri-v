#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string]$Manifest = "striv/tests/fixtures/assets/shader_manifest/assets.toml",
    [string]$Output = "/tmp/striv-assets",
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "../..")

$cmdArgs = @(
    'run', '--project', 'striv/projects/StriV.AssetTool/StriV.AssetTool.csproj', '--', 'build-assets',
    '--manifest', $Manifest,
    '--output', $Output
)

if ($ExtraArgs) {
    $cmdArgs += $ExtraArgs
}

Write-Host ("Running: dotnet " + ($cmdArgs -join ' '))
Push-Location $repoRoot
try {
    & dotnet @cmdArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
