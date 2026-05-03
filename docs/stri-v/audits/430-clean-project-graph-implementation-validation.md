# 430 — Clean project graph implementation validation

## 1. Files changed
- Added `striv/StriV.Core.slnx`.
- Added clean projects under `striv/projects/*` for core/engine/AP/CoreSmoke.
- Added shared profile `striv/build/StriV.Core.Profile.props`.
- Added AP support target `striv/build/StriV.AssemblyProcessor.targets`.
- Added scripts `striv/build/striv-build-core.sh` and `striv/build/striv-build-core.ps1`.
- Added `striv/Directory.Packages.props` importing central package versions.
- Updated `docs/stri-v/building-core.md`.

## 2. Clean graph design
- Solution: `striv/StriV.Core.slnx`.
- Projects: clean `Microsoft.NET.Sdk`, explicit `AssemblyName` and `RootNamespace`, explicit source globs into `sources/` and `samples/`, and linked `sources/shared/SharedAssemblyInfo.cs` for engine/core libs.
- References/packages: explicit `ProjectReference` and minimal package set from legacy csproj/audit.
- Constants: centralized in `striv/build/StriV.Core.Profile.props` for Linux/Desktop/SDL/Vulkan + no shader compiler/audio/VR symbols.
- AP strategy: source-build `Stride.Core.AssemblyProcessor` clean project, then per-project post-build AP invocation through `striv/build/StriV.AssemblyProcessor.targets` and `StriVAssemblyProcessorOptions`.

## 3. Difference from legacy graph
- No import of Stride custom SDK props/targets.
- No graphics API inner-build machinery.
- No pack/payload item groups.
- No `deps/AssemblyProcessor` runtime payload dependency.
- Shader compiler/audio/VR/old Stride.Physics/editor/assets are not pulled into the clean graph.

## 4. Validation results
1) `./striv/build/striv-build-core.sh`
- Exit code: 1
- First meaningful error: `NU1015 ... PackageReference ... do not have a version specified` in `Stride.Core.AssemblyProcessor`.
- Pass/fail: Fail
- Output truncated: No

2) `./striv/build/striv-build-core.sh` (after adding `striv/Directory.Packages.props` and AP linked core attribute files)
- Exit code: 1
- First meaningful blocker: `/bin/bash: xxd: command not found` during AP PE-header validation step in script.
- Pass/fail: Fail
- Output truncated: Yes (tool output token cap); AP build itself completed with warnings.

## 5. First blocker analysis
- Exact project: `striv/projects/Stride.Core/Stride.Core.csproj`.
- Error: duplicate assembly attributes from generated `obj/.../Stride.Core.AssemblyInfo.cs` vs linked shared assembly info (`CS0579`).
- Missing/incorrect setup: clean projects currently still auto-generate assembly attributes while also linking shared assembly metadata source.
- Smallest next repair: set `GenerateAssemblyInfo=false` in clean profile (or for affected projects) and rerun to confirm next blocker.

## 6. Visual Studio expectation
- The clean graph should be VS-friendly because it is plain SDK-style with explicit references and without custom SDK import side effects.
- Risk: large out-of-tree source globs can still impact design-time indexing/perf and can require careful `Compile Remove` tuning for platform-specific files.
- Local validation: open `striv/StriV.Core.slnx` in VS, restore, build `Stride.Core.AssemblyProcessor`, then build `StriV.CoreSmoke`/`Stride.Engine` in Debug.

## 7. Worktree status
Command: `git status --short`

Output:
```text
 M docs/stri-v/building-core.md
?? striv/
```

## 8. Recommended next task
M3c clean graph repair for first blocker: finalize script portability fix and rerun full clean graph build to surface the first true compile/reference blocker, then patch the minimal offending project include/reference.


## Addendum after script portability fix
- Command: `./striv/build/striv-build-core.sh`
- Exit code: 1
- First meaningful error: `CS0579 Duplicate Assembly* attribute` in `Stride.Core`.
- Output truncated: Yes (token cap), but first build-blocking error was captured.
