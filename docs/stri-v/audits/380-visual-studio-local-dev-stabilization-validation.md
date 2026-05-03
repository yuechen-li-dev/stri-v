# M2b validation: Visual Studio local-dev stabilization for `build/StriV.Core.slnx`

## 1) Files changed

- `build/StriV.Core.props`
- `build/striv-vs-prepare-core.ps1`
- `build/striv-prepare-core.sh`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/380-visual-studio-local-dev-stabilization-validation.md`

## 2) Profile props design

- **Path:** `build/StriV.Core.props`
- **Included properties:**
  - `StridePlatforms=Linux` (only when empty)
  - `StrideGraphicsApis=Vulkan` (only when empty)
  - `StrideIncludeShaderCompiler=false` (only when empty)
  - `StrideIncludeAudio=false` (only when empty)
  - `StrideIncludeVirtualReality=false` (only when empty)
  - `StrideAssemblyProcessorFramework=net10.0` (only when empty)
  - `StrideAssemblyProcessorHash=sourcebuild` (only when empty)
- **Why `StrideAssemblyProcessorBasePath` is script-provided:** base path must match the actual source-built AP output folder for the selected configuration and must be absolute + trailing slash, which is runtime/environment-derived.
- **Legacy contamination avoidance:** profile is opt-in and not globally imported from repository-wide props; legacy `build/Stride.sln` behavior remains untouched.

## 3) Prep script design

### `build/striv-vs-prepare-core.ps1`

- **Parameters:**
  - `-Configuration` (`Debug|Release`, default `Debug`)
  - `-Build` (optional, default restore-only)
- **AP build behavior:** builds `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj` with `-f net10.0` and selected configuration.
- **AP validation behavior:**
  - verifies DLL exists
  - verifies DLL size is > 1024 bytes
  - checks file is not Git LFS pointer content
  - checks first bytes are `MZ`
- **Restore command:** restores `build/StriV.Core.slnx` with explicit Stri-V Core properties including script-computed absolute trailing-slash `StrideAssemblyProcessorBasePath`.
- **Optional build behavior:** if `-Build` passed, runs `dotnet build build/StriV.Core.slnx` with same properties.
- **Printed instructions:** tells user to open `build/StriV.Core.slnx`, retry restore/obj cleanup on stale design-time errors, and keep CLI scripts authoritative.

### Optional helper `build/striv-prepare-core.sh`

- Mirrors the PowerShell flow for Linux/dev CLI parity.
- Supports `Debug|Release` positional configuration and optional `--build`.

## 4) Validation results

1. **Command**
   - `dotnet sln build/StriV.Core.slnx list`
   - **Exit code:** 0
   - **First meaningful warning/error:** none
   - **Classification:** pass
   - **Output truncated:** no

2. **Command**
   - `dotnet build sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj -c Debug -f net10.0`
   - **Exit code:** 0
   - **First meaningful warning/error:** `CS1030 #warning: 'PERF: Do not copy byte-for-byte.'`
   - **Classification:** pass (warnings present)
   - **Output truncated:** yes (tool output truncated)

3. **Command**
   - `./build/striv-prepare-core.sh` (first attempt)
   - **Exit code:** 127
   - **First meaningful warning/error:** `xxd: command not found`
   - **Classification:** fail (script dependency issue fixed in follow-up edit)
   - **Output truncated:** no

4. **Command**
   - `pwsh ./build/striv-vs-prepare-core.ps1`
   - **Exit code:** 127
   - **First meaningful warning/error:** `pwsh: command not found`
   - **Classification:** warning (environment limitation: PowerShell unavailable in sandbox)
   - **Output truncated:** no

5. **Command**
   - `./build/striv-prepare-core.sh` (after fix)
   - **Exit code:** 0
   - **First meaningful warning/error:** NU1901 vulnerability warnings from NuGet packages during restore
   - **Classification:** pass (warnings present)
   - **Output truncated:** no

6. **Command**
   - `AP_BASE="$(realpath sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0)/"; dotnet restore build/StriV.Core.slnx -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideIncludeShaderCompiler=false -p:StrideIncludeAudio=false -p:StrideIncludeVirtualReality=false -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath="$AP_BASE" -p:StrideAssemblyProcessorHash=sourcebuild`
   - **Exit code:** 0
   - **First meaningful warning/error:** NU1901 vulnerability warnings from NuGet packages
   - **Classification:** pass (warnings present)
   - **Output truncated:** no

## 5) Visual Studio local validation checklist

- [ ] Close Visual Studio completely.
- [ ] Run `./build/striv-vs-prepare-core.ps1` (or `-Configuration Release` if needed).
- [ ] Open `build/StriV.Core.slnx` in Visual Studio.
- [ ] Confirm build output does not unexpectedly drift to Direct3D11 profile paths.
- [ ] Confirm `net10.0` restore assets exist for active projects.
- [ ] Confirm projects display expected source/code nodes in Solution Explorer.
- [ ] Build `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj` (or selected core projects).
- [ ] If errors persist, capture the first error and effective properties (or VS/MSBuild binlog) for diagnosis.

## 6) Known limitations

- Visual Studio behavior cannot be fully validated inside this Linux sandbox.
- Solution-level/design-time behavior may still differ from CLI behavior.
- CLI scripts remain the authoritative golden path.
- Duplicate AssemblyProcessor payload warnings may persist pending deeper cleanup.
- Current profile intentionally forces Linux/Vulkan even on Windows VS to match validated Stri-V Core spine.

## 7) Worktree status

`git status --short` after changes:

```text
 M docs/stri-v/building-core.md
?? build/StriV.Core.props
?? build/striv-prepare-core.sh
?? build/striv-vs-prepare-core.ps1
?? docs/stri-v/audits/380-visual-studio-local-dev-stabilization-validation.md
```

## 8) Recommended next task

Prep script/profile CLI validation succeeded in sandbox. **Recommended next task:** user-performed local Visual Studio validation on Windows using `build/striv-vs-prepare-core.ps1`, then capture any remaining design-time drift with binlog/effective-properties evidence.
