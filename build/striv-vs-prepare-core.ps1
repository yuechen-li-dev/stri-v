[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Build
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir '..'))
$ApProject = Join-Path $RepoRoot 'sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj'
$Solution = Join-Path $RepoRoot 'build/StriV.Core.slnx'

Write-Host "[striv] Repo root: $RepoRoot"
Write-Host "[striv] Building AssemblyProcessor ($Configuration / net10.0)..."
& dotnet build $ApProject -c $Configuration -f net10.0

$ApOutput = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "sources/core/Stride.Core.AssemblyProcessor/bin/$Configuration/net10.0"))
if (-not $ApOutput.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
    $ApOutput = $ApOutput + [System.IO.Path]::DirectorySeparatorChar
}

$ApDll = Join-Path $ApOutput 'Stride.Core.AssemblyProcessor.dll'
if (-not (Test-Path -LiteralPath $ApDll)) {
    throw "AssemblyProcessor output missing: $ApDll"
}

$fileInfo = Get-Item -LiteralPath $ApDll
if ($fileInfo.Length -le 1024) {
    throw "AssemblyProcessor output is too small ($($fileInfo.Length) bytes): $ApDll"
}

$firstText = Get-Content -LiteralPath $ApDll -Encoding UTF8 -TotalCount 1 -ErrorAction SilentlyContinue
if ($firstText -like 'version https://git-lfs.github*') {
    throw "AssemblyProcessor output appears to be a Git LFS pointer: $ApDll"
}

$bytes = [System.IO.File]::ReadAllBytes($ApDll)
if ($bytes.Length -lt 2 -or $bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) {
    throw "AssemblyProcessor output is not a valid PE/MZ binary: $ApDll"
}

$restoreArgs = @(
    $Solution,
    "-p:Configuration=$Configuration",
    "-p:StrideAssemblyProcessorBasePath=$ApOutput"
)

Write-Host "[striv] Restoring StriV.Core.slnx with repo-visible Stri-V Core profile..."
& dotnet restore @restoreArgs

if ($Build) {
    Write-Host "[striv] Building StriV.Core.slnx with same profile properties..."
    & dotnet build $Solution -c $Configuration "-p:StrideAssemblyProcessorBasePath=$ApOutput"
}

Write-Host ''
Write-Host 'Open build/StriV.Core.slnx in Visual Studio now.'
Write-Host 'Close and reopen Visual Studio after prep so design-time restore re-evaluates the imported profile.'
Write-Host 'If VS still shows stale errors, close VS, delete affected obj/bin folders for the affected Stri-V projects, rerun prep, and reopen.'
Write-Host 'Use Stri-V scripts for authoritative CLI validation.'
