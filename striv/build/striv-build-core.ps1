$ErrorActionPreference='Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$apProj = Join-Path $root 'striv/projects/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj'
$soln = Join-Path $root 'striv/StriV.Core.slnx'
$apDll = Join-Path $root 'striv/projects/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll'
Write-Host "+ dotnet build $apProj -c Debug"; dotnet build $apProj -c Debug
if (!(Test-Path $apDll)) { throw "AP missing: $apDll" }
if ((Get-Item $apDll).Length -le 1024) { throw "AP too small" }
$head = [System.IO.File]::ReadAllBytes($apDll)[0..1]
if (!($head[0] -eq 0x4D -and $head[1] -eq 0x5A)) { throw 'AP is not MZ PE' }
Write-Host "+ dotnet restore $soln"; dotnet restore $soln
Write-Host "+ dotnet build $soln -c Debug"; dotnet build $soln -c Debug /p:StriVAssemblyProcessorPath=$apDll
