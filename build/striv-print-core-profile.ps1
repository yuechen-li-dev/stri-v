[CmdletBinding()]
param(
    [string]$Project = 'samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj'
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir '..'))
$ProjectPath = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Project))

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Project not found: $ProjectPath"
}

$properties = @(
    'StridePlatforms',
    'StrideGraphicsApis',
    'StrideIncludeShaderCompiler',
    'StrideIncludeAudio',
    'StrideIncludeVirtualReality',
    'StrideAssemblyProcessorFramework',
    'StrideAssemblyProcessorHash',
    'StrideAssemblyProcessorBasePath',
    'Configuration'
)

Write-Host "[striv] Effective Stri-V Core profile for $ProjectPath"
& dotnet msbuild $ProjectPath -nologo "-getProperty:$($properties -join ',')"
