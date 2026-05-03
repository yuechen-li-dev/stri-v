# 110 – Engine foundation M1b validation

## 1) Files changed

- `build/StriV.EngineFoundation.M1b.slnf`
- `build/striv-build-engine-foundation-m1b.sh`
- `build/striv-build-engine-foundation-m1b.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/110-engine-foundation-m1b-validation.md`

## 2) Solution filter contents

- Base solution: `Stride.sln`.
- Explicit included projects:
  1. `sources/core/Stride.Core/Stride.Core.csproj`
  2. `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
  3. `sources/core/Stride.Core.IO/Stride.Core.IO.csproj`
  4. `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
  5. `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
  6. `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj`
  7. `sources/engine/Stride/Stride.csproj`
- Excluded systems remain excluded from explicit `.slnf` entries: `Stride.Engine`, graphics/rendering/input/audio/VR/shader compiler/editor/assets/samples/tests/mobile slices.
- `Stride.FreeImage` was **not** explicitly added to the filter; it was restored and built transitively via `Stride.csproj`.

## 3) Script design

Both scripts mirror the established M1a bootstrap flow with M1b-specific naming and solution filter path.

- Repo root detection:
  - Bash: derives `SCRIPT_DIR` from `${BASH_SOURCE[0]}` then resolves `REPO_ROOT` as parent.
  - PowerShell: derives `$ScriptDir` from `$MyInvocation.MyCommand.Path` and resolves parent as `$RepoRoot`.
- Configuration handling:
  - Bash: positional `Debug` (default) or `Release`; rejects other values; forwards remaining args to final `dotnet build`.
  - PowerShell: `-Configuration Debug|Release` with default `Debug`; `ValueFromRemainingArguments` forwarded to final `dotnet build`.
- AssemblyProcessor bootstrap and validation:
  - Builds `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj` first.
  - Validates output under `sources/core/Stride.Core.AssemblyProcessor/bin/<Configuration>/net10.0/`.
  - Validates DLL existence, size > 1024 bytes, non-LFS-pointer prefix, and `MZ` header.
- M1b build invocation:
  - Builds `build/StriV.EngineFoundation.M1b.slnf`.
  - Passes:
    - `-p:StrideAssemblyProcessorFramework=net10.0`
    - `-p:StrideAssemblyProcessorBasePath=<absolute AP output dir with trailing slash>`
    - `-p:StrideAssemblyProcessorHash=sourcebuild`
  - Prints repo root, configuration, AP output path, filter path, and success status.

## 4) Validation results

| Command | Exit code | First meaningful warning/error | Classification | Output truncated |
| --- | ---: | --- | --- | --- |
| `./build/striv-build-engine-foundation-m1b.sh` | 0 | warning: repo has no remote / source link empty (build still succeeds) | Pass | Yes (tool output capped; build completion line captured) |
| `./build/striv-build-engine-foundation-m1b.sh Release` | 0 | warning: repo has no remote / source link empty (build still succeeds) | Pass | Yes (tool output capped; build completion line captured) |
| `pwsh ./build/striv-build-engine-foundation-m1b.ps1` | 127 | `/bin/bash: line 1: pwsh: command not found` | Not executed in this Linux environment | No |

## 5) FreeImage/native dependency observations

- `Stride.FreeImage` was restored transitively (not explicitly listed in the `.slnf`).
- No restore/build failure was caused by `Stride.FreeImage` in Debug or Release.
- No Linux-specific native payload blocker surfaced during this compile-only validation.
- Representative FreeImage-related output observed as warnings (e.g., nullable warning in `FreeImageWrapper.cs`), not errors.

## 6) M1b verdict

| Candidate                               | Verdict | Current blocker | Next action |
| --------------------------------------- | ------- | --------------- | ----------- |
| `build/StriV.EngineFoundation.M1b.slnf` | Adopt | None in Debug/Release compile validation | Proceed to M1c-prep for platform/window/input slice |

## 7) Worktree status

Command run:

```bash
git status --short
```

Result at report time:

```text
 M docs/stri-v/building-core.md
?? build/StriV.EngineFoundation.M1b.slnf
?? build/striv-build-engine-foundation-m1b.ps1
?? build/striv-build-engine-foundation-m1b.sh
?? docs/stri-v/audits/110-engine-foundation-m1b-validation.md
```

## 8) Recommended next task

M1b builds in Debug and Release in this Linux validation, so the next recommended task is:

- **M1c-prep for platform/window/input slice**.
