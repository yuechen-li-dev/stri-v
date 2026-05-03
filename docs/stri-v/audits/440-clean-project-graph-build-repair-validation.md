# 440 Clean Project Graph Build Repair Validation

## 1) Files changed
- `striv/build/StriV.Core.Profile.props`
- `striv/StriV.Core.slnx`
- `striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`

## 2) Duplicate assembly attribute diagnosis
- First failing project: `striv/projects/Stride.Core/Stride.Core.csproj`.
- Duplicate attributes observed:
  - `AssemblyFileVersionAttribute`
  - `AssemblyInformationalVersionAttribute`
  - `AssemblyVersionAttribute`
- Generated file source: `striv/projects/Stride.Core/obj/Debug/net10.0/Stride.Core.AssemblyInfo.cs`.
- Existing source declarations: `sources/shared/SharedAssemblyInfo.cs` with `[assembly: AssemblyVersion(...)]`, `[assembly: AssemblyFileVersion(...)]`, `[assembly: AssemblyInformationalVersion(...)]`.
- Chosen fix: set `GenerateAssemblyInfo` to `false` in shared clean profile (`striv/build/StriV.Core.Profile.props`).
- Safety rationale: clean SDK projects in this migration graph are using shared Stride metadata files; disabling SDK auto-generation avoids duplication while preserving intended explicit version metadata from shared source.

## 3) Clean graph changes
- Profile change: added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` in clean profile.
- Scope choice: global (shared clean profile), not per-project.
- Old legacy project files: untouched.

## 4) Test infrastructure
- Added test project: `striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`.
- Added to solution: yes (`striv/StriV.Core.slnx`).
- Framework: xUnit + `Microsoft.NET.Test.Sdk`.
- Tests added (`CleanGraphSmokeTests`):
  - `AssemblyIdentityTypesAreReachable`: compile-time/type identity reachability for `Stride.Core.Utilities` and `Stride.Core.Mathematics.Vector3`.
  - `CleanProfileConstantsAreDefined`: verifies key clean-profile constants are defined for the clean graph test project.
  - `CleanGraphProjectReferencesResolve`: compile-time linkage to `Stride.Engine.Game` and `Stride.BepuPhysics.BepuSimulation`.
- Intentionally not proven:
  - runtime behavior,
  - rendering/audio/VR/shader compiler integration,
  - asset/content pipelines,
  - broad regression parity with legacy Stride tests.
- Coverage stance: diagnostic seeds only; coverage expansion intentionally deferred.

## 5) Validation results
1. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `1`
   - First meaningful error (initial run):
     - `error CS0579` duplicate assembly attributes in `striv/projects/Stride.Core/obj/Debug/net10.0/Stride.Core.AssemblyInfo.cs`.
   - Pass/fail: fail.
   - Output truncated: yes.

2. Command: `./striv/build/striv-build-core.sh` (after profile fix + test project add)
   - Exit code: `1`
   - First meaningful error:
     - `NU1008` in `striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj` due to central package management requiring no `Version` in `PackageReference` items.
   - Pass/fail: fail.
   - Output truncated: yes.

3. Command: `./striv/build/striv-build-core.sh` (after NU1008 repair)
   - Exit code: `1`
   - First meaningful error:
     - `MSB3073` in `striv/build/StriV.AssemblyProcessor.targets` line 3: assembly processor command exits with code `131` while processing `Stride.Core.dll`.
   - Pass/fail: fail.
   - Output truncated: yes.

## 6) First new blocker
- Project: `striv/projects/Stride.Core/Stride.Core.csproj` during target in `striv/build/StriV.AssemblyProcessor.targets`.
- Error: `MSB3073` line 3, command exited with code `131`.
- Likely cause: runtime crash/fault in `Stride.Core.AssemblyProcessor` execution against built `Stride.Core.dll` under current clean graph/profile context.
- Smallest proposed next repair:
  1. Run the exact failing `dotnet ...Stride.Core.AssemblyProcessor.dll ...Stride.Core.dll --auto-module-initializer --serialization` command directly.
  2. Capture stderr/stack trace and isolate first crashing processor stage.
  3. Apply narrow fix in clean assembly-processor integration path (arguments/targets/inputs), without broad project graph edits.

## 7) Worktree status
- Command: `git status --short`
- Result:
  - `M striv/StriV.Core.slnx`
  - `M striv/build/StriV.Core.Profile.props`
  - `?? striv/tests/`

## 8) Recommended next task
- **M3d clean graph repair for first blocker**: focus on assembly processor exit code `131` investigation and narrow repair.
