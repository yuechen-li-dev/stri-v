#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path

$ApProject = Join-Path $RepoRoot "sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj"
$ApOutputDirNoSlash = Join-Path $RepoRoot "sources/core/Stride.Core.AssemblyProcessor/bin/$Configuration/net10.0"
$ApOutputDir = "$ApOutputDirNoSlash/"
$ApDll = Join-Path $ApOutputDirNoSlash "Stride.Core.AssemblyProcessor.dll"
$M1fSlnf = Join-Path $RepoRoot "build/StriV.Engine.Bepu.M1f.slnf"

Write-Host "[striv-engine-bepu-m1f] Repo root: $RepoRoot"
Write-Host "[striv-engine-bepu-m1f] Configuration: $Configuration"
Write-Host "[striv-engine-bepu-m1f] Platform: Linux"
Write-Host "[striv-engine-bepu-m1f] Graphics API: Vulkan"
Write-Host "[striv-engine-bepu-m1f] Shader compiler: disabled"
Write-Host "[striv-engine-bepu-m1f] Audio: disabled"
Write-Host "[striv-engine-bepu-m1f] VR: disabled"
Write-Host "[striv-engine-bepu-m1f] AssemblyProcessor output directory: $ApOutputDir"
Write-Host "[striv-engine-bepu-m1f] M1f solution filter: $M1fSlnf"

Write-Host "[striv-engine-bepu-m1f] Building AssemblyProcessor..."
& dotnet build $ApProject -c $Configuration -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Test-Path -LiteralPath $ApDll -PathType Leaf)) {
    Write-Error "AssemblyProcessor DLL not found at: $ApDll"
}

$fileInfo = Get-Item -LiteralPath $ApDll
if ($fileInfo.Length -le 1024) {
    Write-Error "AssemblyProcessor DLL is unexpectedly small ($($fileInfo.Length) bytes): $ApDll"
}

$bytes = [System.IO.File]::ReadAllBytes($ApDll)
$prefixLength = [Math]::Min(64, $bytes.Length)
$prefixText = [System.Text.Encoding]::UTF8.GetString($bytes, 0, $prefixLength)
if ($prefixText.StartsWith("version https://git-lfs.github")) {
    Write-Error "AssemblyProcessor DLL appears to be a Git LFS pointer file: $ApDll"
}

if ($bytes.Length -lt 2 -or $bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) {
    $found = if ($bytes.Length -ge 2) { "{0:X2}{1:X2}" -f $bytes[0], $bytes[1] } else { "<none>" }
    Write-Error "AssemblyProcessor DLL is not a valid PE payload (expected MZ header, got $found): $ApDll"
}

Write-Host "[striv-engine-bepu-m1f] AssemblyProcessor payload validation passed (size=$($fileInfo.Length) bytes, header=MZ)."

Write-Host "[striv-engine-bepu-m1f] Building Stri-V Engine Bepu M1f..."
$buildArgs = @(
    "build", $M1fSlnf,
    "-c", $Configuration,
    "-v", "minimal",
    "-p:StridePlatforms=Linux",
    "-p:StrideGraphicsApis=Vulkan",
    "-p:StrideIncludeShaderCompiler=false",
    "-p:StrideIncludeAudio=false",
    "-p:StrideIncludeVirtualReality=false",
    "-p:StrideAssemblyProcessorFramework=net10.0",
    "-p:StrideAssemblyProcessorBasePath=$ApOutputDir",
    "-p:StrideAssemblyProcessorHash=sourcebuild"
) + $ExtraArgs

& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[striv-engine-bepu-m1f] Build completed successfully."
exit 0
