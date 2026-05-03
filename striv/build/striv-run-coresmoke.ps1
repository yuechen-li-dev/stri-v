param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$BuildScript = Join-Path $Root 'striv/build/striv-build-core.ps1'
$CoreSmokeDll = Join-Path $Root "striv/projects/StriV.CoreSmoke/bin/$Configuration/net10.0/StriV.CoreSmoke.dll"

Write-Host "Repo root: $Root"
Write-Host "Configuration: $Configuration"
Write-Host "CoreSmoke DLL: $CoreSmokeDll"

& $BuildScript -Configuration $Configuration

if (-not (Test-Path $CoreSmokeDll)) {
    Write-Host 'Runtime failure classification: missing build output'
    Write-Error "CoreSmoke DLL not found: $CoreSmokeDll"
}

$dotnet = (Get-Command dotnet).Source
$args = @($CoreSmokeDll)
Write-Host "Run command: $dotnet $CoreSmokeDll"
Write-Host 'Timeout behavior: enforcing 20s timeout via Start-Process/Wait-Process.'

$process = Start-Process -FilePath $dotnet -ArgumentList $args -NoNewWindow -PassThru
$timedOut = $false
try {
    Wait-Process -Id $process.Id -Timeout 20
} catch {
    $timedOut = $true
    try { Stop-Process -Id $process.Id -Force } catch {}
}

if ($timedOut) {
    Write-Host 'Runtime exit code: 124'
    Write-Host 'Runtime failure classification: timeout/hang'
    exit 124
}

$exitCode = $process.ExitCode
Write-Host "Runtime exit code: $exitCode"
if ($exitCode -eq 0) {
    Write-Host 'Runtime status: success'
    exit 0
}

Write-Host 'Runtime failure classification hints:'
Write-Host '- SDL/display/X11 unavailable'
Write-Host '- Vulkan loader/ICD/device failure'
Write-Host '- missing native library'
Write-Host '- content/shader/effect runtime failure'
Write-Host '- managed engine/runtime exception'
Write-Host 'Runtime status: failure'
exit $exitCode
