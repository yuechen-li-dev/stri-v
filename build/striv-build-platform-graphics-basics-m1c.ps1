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
$M1cSlnf = Join-Path $RepoRoot "build/StriV.PlatformGraphicsBasics.M1c.slnf"

Write-Host "[striv-platform-graphics-basics-m1c] Repo root: $RepoRoot"
Write-Host "[striv-platform-graphics-basics-m1c] Configuration: $Configuration"
Write-Host "[striv-platform-graphics-basics-m1c] Platform: Linux"
Write-Host "[striv-platform-graphics-basics-m1c] Graphics API: Vulkan"
Write-Host "[striv-platform-graphics-basics-m1c] AssemblyProcessor project: $ApProject"
Write-Host "[striv-platform-graphics-basics-m1c] AssemblyProcessor output directory: $ApOutputDir"
Write-Host "[striv-platform-graphics-basics-m1c] M1c solution filter: $M1cSlnf"

Write-Host "[striv-platform-graphics-basics-m1c] Building AssemblyProcessor..."
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

Write-Host "[striv-platform-graphics-basics-m1c] AssemblyProcessor payload validation passed (size=$($fileInfo.Length) bytes, header=MZ)."

Write-Host "[striv-platform-graphics-basics-m1c] Building Stri-V Platform + Graphics Basics M1c..."
$buildArgs = @(
    "build", $M1cSlnf,
    "-c", $Configuration,
    "-v", "minimal",
    "-p:StridePlatforms=Linux",
    "-p:StrideGraphicsApis=Vulkan",
    "-p:StrideAssemblyProcessorFramework=net10.0",
    "-p:StrideAssemblyProcessorBasePath=$ApOutputDir",
    "-p:StrideAssemblyProcessorHash=sourcebuild"
) + $ExtraArgs

& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[striv-platform-graphics-basics-m1c] Build completed successfully."
exit 0
