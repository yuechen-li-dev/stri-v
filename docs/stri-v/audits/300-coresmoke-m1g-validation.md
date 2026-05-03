# Stri-V M1g CoreSmoke validation

## 1) Files changed
- `build/Stride.sln`
- `build/StriV.CoreSmoke.M1g.slnf`
- `build/striv-build-coresmoke-m1g.sh`
- `build/striv-build-coresmoke-m1g.ps1`
- `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`
- `samples/StriV/CoreSmoke/Program.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/300-coresmoke-m1g-validation.md`

## 2) Smoke project design
- Path: `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`.
- Output type: `Exe` (not `WinExe`).
- Target framework: `net10.0` (not `net10.0-windows`).
- Project references: direct `ProjectReference` to `sources/engine/Stride.Engine/Stride.Engine.csproj` only.
- Asset/editor/Game Studio/source YAML avoidance:
  - no `.sdpkg`, `.sdscene`, `.sdproj`
  - no `Stride.Assets`, `Stride.Core.Assets`, editor/presentation references
  - code-first executable only.
- Runtime-only reference posture: explicit direct reference is runtime `Stride.Engine`; no explicit asset/editor packages added.

## 3) Program design
- Entry point shape:
  - `using Stride.Engine;`
  - `using var game = new Game();`
  - `game.Run();`
- Uses `Game` directly (no subclass needed for M1g compile closure).
- Does not load assets.
- Does not use audio/VR/shader compiler APIs.
- Scripts perform build-only by default; no automatic run behavior is introduced.

## 4) Solution filter contents
- Base solution in filter: `Stride.sln` (resolved under `build/` as `build/Stride.sln`).
- Included projects are the full explicit M1f set plus:
  - `..\samples\StriV\CoreSmoke\StriV.CoreSmoke.csproj`.
- Exclusion confirmation: no old `Stride.Physics`, no Bepu companion modules, no sample/test/editor/assets/presentation/mobile/video/voxels/SpriteStudio/launcher/metrics/packaging projects were explicitly added.

## 5) Script design
- Mirror of M1f behavior:
  - robust repo-root detection relative to script location
  - configuration handling (`Debug` default, `Release` optional)
  - forward extra build args
  - AssemblyProcessor source-build first
  - payload validation (exists, >1024 bytes, non-LFS pointer, `MZ` header)
  - Linux/Vulkan route + shader/audio/VR opt-out properties
  - AP routing via `StrideAssemblyProcessorFramework`, `StrideAssemblyProcessorBasePath` (absolute trailing slash), and `StrideAssemblyProcessorHash=sourcebuild`.
- Both scripts are build-only by default (no run mode).

## 6) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `./build/striv-build-coresmoke-m1g.sh` (first attempt) | 1 | `MSB5028`: sample project was not in base solution file | Fail | No |
| `dotnet sln build/Stride.sln add samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj` | 0 | none | Pass | No |
| `./build/striv-build-coresmoke-m1g.sh` (Debug rerun) | 0 | warnings only (existing NU/CA/CS warnings in graph) | Pass | Yes (tool output truncation) |
| `./build/striv-build-coresmoke-m1g.sh Release` | 0 | warnings only (existing NU/CA/CS warnings in graph) | Pass | Yes (tool output truncation) |
| `pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'` | 127 | `pwsh: command not found` | Not executed (environment) | No |

## 7) Smoke observations
- Smoke executable restored/built as part of M1g Debug+Release successful runs.
- No intentional asset/editor/compiler package references were added to smoke project.
- Smoke project stayed on `net10.0` and avoided `net10.0-windows`.
- Shader/audio/VR opt-outs stayed active through script MSBuild properties.
- No runtime executable launch attempt was made.
- Note: transitive restore/build still includes some asset-related projects in the broader Stride graph (existing behavior), despite smoke project remaining code-first and asset-free.

## 8) M1g verdict

| Candidate                        | Verdict | Current blocker | Next action |
| -------------------------------- | ------- | --------------- | ----------- |
| `build/StriV.CoreSmoke.M1g.slnf` | Adopt after repair | Initial blocker repaired by adding smoke project to `build/Stride.sln`; remaining warnings are non-blocking | Proceed to M1h-prep for opt-in runtime run smoke |

## 9) Worktree status
`git status --short` at end:

```text
 M build/Stride.sln
 M deps/AssemblyProcessor/net10.0/Mono.Cecil.Mdb.dll
 M deps/AssemblyProcessor/net10.0/Mono.Cecil.Pdb.dll
 M deps/AssemblyProcessor/net10.0/Mono.Cecil.Rocks.dll
 M deps/AssemblyProcessor/net10.0/Mono.Cecil.dll
 M deps/AssemblyProcessor/net10.0/Mono.Options.dll
 M deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.dll
 M deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.dll.hash
 M deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.pdb
 M deps/AssemblyProcessor/netstandard2.0/Mono.Cecil.Mdb.dll
 M deps/AssemblyProcessor/netstandard2.0/Mono.Cecil.Pdb.dll
 M deps/AssemblyProcessor/netstandard2.0/Mono.Cecil.Rocks.dll
 M deps/AssemblyProcessor/netstandard2.0/Mono.Cecil.dll
 M deps/AssemblyProcessor/netstandard2.0/Mono.Options.dll
 M deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll
 M deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll.hash
 M deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.pdb
 M docs/stri-v/building-core.md
?? build/StriV.CoreSmoke.M1g.slnf
?? build/striv-build-coresmoke-m1g.ps1
?? build/striv-build-coresmoke-m1g.sh
?? docs/stri-v/audits/300-coresmoke-m1g-validation.md
?? samples/StriV/
```

## 10) Recommended next task
Because M1g builds, recommend **M1h-prep for opt-in runtime run smoke**.
