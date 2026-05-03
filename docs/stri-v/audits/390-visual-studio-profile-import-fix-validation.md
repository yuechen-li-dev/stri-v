# 390 Visual Studio profile import fix validation

## A. Files changed

- `Directory.Build.props`
- `build/StriV.Core.props`
- `build/striv-vs-prepare-core.ps1`
- `build/striv-print-core-profile.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/390-visual-studio-profile-import-fix-validation.md`

## B. Diagnosis

The earlier `striv-vs-prepare-core.ps1` flow restored `build/StriV.Core.slnx` correctly only when the Stri-V Core property set was passed explicitly on the command line. That was enough for CLI restore/build, but it was not enough for Visual Studio design-time restore and project evaluation because VS re-evaluates projects independently after the script exits. When those later evaluations did not receive the Stri-V profile, the repo fell back toward Windows/Direct3D11/default feature behavior, which matches the observed `net10.0` target mismatch, missing reference metadata outputs, unexpected Direct3D11 output paths, partial Solution Explorer display, and AssemblyProcessor path warnings.

In this hardfork, stronger repo-visible Stri-V Core defaults are acceptable because `build/StriV.Core.slnx` is now the primary local-dev solution and `build/Stride.sln` is legacy/reference terrain rather than the authoritative Stri-V development entry point.

## C. Profile import design

The fix imports `build/StriV.Core.props` from root `Directory.Build.props`. That import point is early and repo-visible, so it participates in normal MSBuild and Visual Studio project evaluation for engine projects, the Stri-V sample, and the `.slnx` graph.

`build/StriV.Core.props` now sets these defaults only when callers did not already provide them:

- `StridePlatforms=Linux`
- `StrideGraphicsApis=Vulkan`
- `StrideIncludeShaderCompiler=false`
- `StrideIncludeAudio=false`
- `StrideIncludeVirtualReality=false`
- `StrideAssemblyProcessorFramework=net10.0`
- `StrideAssemblyProcessorHash=sourcebuild`

It also computes a source-build AssemblyProcessor base path at:

- `sources/core/Stride.Core.AssemblyProcessor/bin/$(Configuration)/net10.0/`

The profile uses a Stri-V-local fallback to `Debug` only when `$(Configuration)` is empty during early evaluation, and it sets `StrideAssemblyProcessorBasePath` only when that property is still empty and `Stride.Core.AssemblyProcessor.dll` exists at the computed path. This keeps command-line or CI overrides authoritative.

`build/striv-vs-prepare-core.ps1` still bootstrap-builds AssemblyProcessor first and still restores the `.slnx`, but it now relies on the imported repo-visible defaults instead of resupplying the entire Stri-V property bundle on every restore/build. It still passes the explicit source-built `StrideAssemblyProcessorBasePath` during prep as an extra safety rail.

Effect on legacy `build/Stride.sln`: it now sees the same Stri-V Core defaults unless explicitly overridden, which is acceptable in this hardfork but should still be treated as legacy/reference behavior.

## D. Validation results

### Command

```powershell
dotnet build sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj -c Debug -f net10.0
```

- Exit code: `0`
- First meaningful warning/error: none; build succeeded
- Pass/fail: pass
- Output truncated: no

### Command

```powershell
dotnet msbuild samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj -nologo -getProperty:StridePlatforms,StrideGraphicsApis,StrideIncludeShaderCompiler,StrideIncludeAudio,StrideIncludeVirtualReality,StrideAssemblyProcessorFramework,StrideAssemblyProcessorHash,StrideAssemblyProcessorBasePath,Configuration
```

- Exit code: `0`
- First meaningful warning/error: none; property probe succeeded
- Pass/fail: pass
- Output truncated: no

Observed effective properties:

- `StridePlatforms=Linux`
- `StrideGraphicsApis=Vulkan`
- `StrideIncludeShaderCompiler=false`
- `StrideIncludeAudio=false`
- `StrideIncludeVirtualReality=false`
- `StrideAssemblyProcessorFramework=net10.0`
- `StrideAssemblyProcessorHash=sourcebuild`
- `StrideAssemblyProcessorBasePath=C:\Users\yuech\source\repos\stri-v\build\..\sources\core\Stride.Core.AssemblyProcessor\bin\Debug\net10.0\`
- `Configuration=Debug`

### Command

```powershell
dotnet restore build/StriV.Core.slnx
```

- Exit code: `0`
- First meaningful warning/error: `warning NU1510` on `Stride.Engine.csproj` and `Stride.Graphics.csproj` about prune candidates, followed by existing low-severity `NU1901` vulnerability warnings in asset/task projects
- Pass/fail: pass
- Output truncated: no

### Command

```powershell
dotnet build build/StriV.Core.slnx -c Debug
```

- Exit code: `1`
- First meaningful warning/error: `error NU5019: File not found: 'C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\nuget-icon.png'`
- Pass/fail: fail
- Output truncated: no

Interpretation: the Stri-V profile import behaved correctly enough for restore and property evaluation, but the full solution build is still blocked by a separate packaging-side issue unrelated to the Visual Studio profile visibility fix.

### Command

```powershell
powershell -ExecutionPolicy Bypass -File .\build\striv-vs-prepare-core.ps1
```

- Exit code: `0`
- First meaningful warning/error: existing `NU1510` and `NU1901` restore warnings only; the prep flow completed and printed the Visual Studio reopen guidance
- Pass/fail: pass
- Output truncated: no

## E. Visual Studio local validation instructions

1. Close Visual Studio completely.
2. If the previous session left stale state, delete affected `obj` and `bin` folders for the impacted Stri-V projects.
3. Run `.\build\striv-vs-prepare-core.ps1`.
4. Reopen Visual Studio and open `build\StriV.Core.slnx`.
5. Confirm the projects show normal code/project structure in Solution Explorer.
6. Confirm the effective local-dev profile is Linux/Vulkan and that Direct3D11-oriented output path leakage is gone.
7. Build `samples\StriV\CoreSmoke\StriV.CoreSmoke.csproj` from Visual Studio.
8. If any error remains, capture and report the first meaningful error only.

## F. Remaining risks

- Visual Studio may cache old design-time state and continue to show stale diagnostics until the solution is reopened.
- Some duplicate AssemblyProcessor payload/link warnings may still persist even with the correct profile.
- Root `Directory.Build.props` now affects legacy/reference builds too.
- The Windows local developer profile intentionally uses Linux/Vulkan defaults for Stri-V Core.
- Full `build/StriV.Core.slnx` CLI build is still blocked by the separate `NU5019` missing `nuget-icon.png` packaging issue.

## G. Recommended next task

Local Visual Studio validation by user.
