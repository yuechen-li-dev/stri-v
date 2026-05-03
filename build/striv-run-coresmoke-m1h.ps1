#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
$CoreSmokeDll = Join-Path $RepoRoot "samples/StriV/CoreSmoke/bin/$Configuration/net10.0/StriV.CoreSmoke.dll"

Write-Host "[striv-coresmoke-m1h] Repo root: $RepoRoot"
Write-Host "[striv-coresmoke-m1h] Configuration: $Configuration"

Write-Host "[striv-coresmoke-m1h] Building CoreSmoke via M1g build script..."
& (Join-Path $RepoRoot "build/striv-build-coresmoke-m1g.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path -LiteralPath $CoreSmokeDll -PathType Leaf)) {
    Write-Error "CoreSmoke DLL not found at: $CoreSmokeDll"
}

Write-Host "[striv-coresmoke-m1h] CoreSmoke DLL: $CoreSmokeDll"
$runArgs = @($CoreSmokeDll)
Write-Host "[striv-coresmoke-m1h] Run command: dotnet $CoreSmokeDll"

$process = Start-Process -FilePath "dotnet" -ArgumentList $runArgs -NoNewWindow -PassThru
$timedOut = -not $process.WaitForExit(20000)
if ($timedOut) {
    try { $process.Kill($true) } catch {}
    Write-Host "[striv-coresmoke-m1h] Runtime exit code: 124"
    Write-Error "Runtime failed: timeout reached (possible runtime hang)."
}

$runExit = $process.ExitCode
Write-Host "[striv-coresmoke-m1h] Runtime exit code: $runExit"
if ($runExit -eq 0) {
    Write-Host "[striv-coresmoke-m1h] Runtime smoke passed."
    exit 0
}

Write-Host "[striv-coresmoke-m1h] Runtime failed. Classifying first blocker..."
Write-Host "[striv-coresmoke-m1h] If output contains SDL display initialization failures, classify as environment limitation."
Write-Host "[striv-coresmoke-m1h] If output contains Vulkan loader/ICD/device creation failures, classify as environment limitation."
Write-Host "[striv-coresmoke-m1h] If output contains missing native library errors, classify as environment/native packaging blocker."
Write-Host "[striv-coresmoke-m1h] Otherwise classify as potential engine/runtime blocker."
exit $runExit
