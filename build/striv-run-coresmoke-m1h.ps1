#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$SdlVideoDriver
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
$CoreSmokeDll = Join-Path $RepoRoot "samples/StriV/CoreSmoke/bin/$Configuration/net10.0/StriV.CoreSmoke.dll"

Write-Host "[striv-coresmoke-m1h] Repo root: $RepoRoot"
Write-Host "[striv-coresmoke-m1h] Configuration: $Configuration"
if ([string]::IsNullOrWhiteSpace($SdlVideoDriver)) {
    Write-Host "[striv-coresmoke-m1h] SDL video driver override: <none>"
} else {
    Write-Host "[striv-coresmoke-m1h] SDL video driver override: $SdlVideoDriver"
}

Write-Host "[striv-coresmoke-m1h] Building CoreSmoke via M1g build script..."
& (Join-Path $RepoRoot "build/striv-build-coresmoke-m1g.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path -LiteralPath $CoreSmokeDll -PathType Leaf)) {
    Write-Error "CoreSmoke DLL not found at: $CoreSmokeDll"
}

Write-Host "[striv-coresmoke-m1h] CoreSmoke DLL: $CoreSmokeDll"
$runArgs = @($CoreSmokeDll)
if ([string]::IsNullOrWhiteSpace($SdlVideoDriver)) {
    Write-Host "[striv-coresmoke-m1h] Run command: dotnet $CoreSmokeDll"
    $process = Start-Process -FilePath "dotnet" -ArgumentList $runArgs -NoNewWindow -PassThru
} else {
    Write-Host "[striv-coresmoke-m1h] Run command: SDL_VIDEODRIVER=$SdlVideoDriver dotnet $CoreSmokeDll"
    $process = Start-Process -FilePath "dotnet" -ArgumentList $runArgs -NoNewWindow -PassThru -Environment @{ SDL_VIDEODRIVER = $SdlVideoDriver }
}

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
Write-Host "[striv-coresmoke-m1h] If output contains 'x11 not available' or display/window allocation failures, classify as environment limitation."
Write-Host "[striv-coresmoke-m1h] If output contains dummy/offscreen SDL driver unavailable errors, classify as headless probe limitation (non-authoritative for engine runtime)."
Write-Host "[striv-coresmoke-m1h] If output contains Vulkan loader/ICD/device creation failures, classify as environment limitation."
Write-Host "[striv-coresmoke-m1h] If output contains graphics device/swapchain creation failures, classify as environment limitation unless local desktop reproduction indicates engine issue."
Write-Host "[striv-coresmoke-m1h] If output contains missing native library errors, classify as environment/native packaging blocker."
Write-Host "[striv-coresmoke-m1h] If output contains managed engine/runtime exceptions, classify as potential engine/runtime blocker."
exit $runExit
