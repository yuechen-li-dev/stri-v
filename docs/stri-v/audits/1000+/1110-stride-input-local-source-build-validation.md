# 1110 — Stride.Input project-local source build validation

## 1) Files changed
- `striv/projects/Stride.Input/Stride.Input.csproj`
- `striv/projects/Stride.Input/VirtualButtonConfigShim.cs`

## 2) Goal recap
Stri-V is moving active compile ownership into `striv/projects/**` while keeping `sources/**` as upstream/reference terrain. This task pilots that model for `Stride.Input` by switching compile ownership to the copied/culled `striv/projects/Stride.Input` tree.

## 3) Project source switch
- **Old compile shape:** explicit compile include from upstream tree:
  - `../../..//sources/engine/Stride.Input/**/*.cs`
  - with old `Compile Remove` exclusions against upstream Android/UWP and Windows DirectInput/XInput paths.
- **New compile shape:** explicit local compile include:
  - `**/*.cs` (project-local tree), with bin/obj excluded.
- **Upstream source status after change:** `sources/engine/Stride.Input/**/*.cs` is no longer compiled by this project.
- **Shared/link files retained:** shared assembly info link remains:
  - `../../../sources/shared/SharedAssemblyInfo.cs` linked as `Properties/SharedAssemblyInfo.cs`.

## 4) Human cull audit
- **Observed as removed in local copy:** Android, iOS, UWP, VirtualButton folders are absent from `striv/projects/Stride.Input`.
- **Kept removed:** Android/iOS/UWP stayed removed.
- **VirtualButton handling:** full VirtualButton system was not restored. Instead, one minimal compatibility shim (`VirtualButtonConfigSet` + `VirtualButtonConfig`) was added locally to satisfy `InputManager` API/type references without reintroducing platform/runtime virtual-button implementation.
- **Stale references removed/updated:** csproj no longer compiles from upstream `sources/engine/Stride.Input`; stale upstream `Compile Remove` entries were removed and replaced with project-local remove entries for:
  - `Windows/InputSourceWindowsDirectInput.cs`
  - `Windows/InputSourceWindowsXInput.cs`

## 5) Desktop input scope (current)
Current scope in this pilot remains:
- Linux desktop / SDL path retained.
- Windows desktop path retained, with RawInput path available.
- Simulated input retained.
- Android/iOS/UWP excluded.
- VirtualButton runtime system excluded (API compatibility shim only).
- DirectInput/XInput input source entries excluded from compile for this clean project unless later proven required.

## 6) Build/test results

1. Command:
   - `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-input-local-build.log`
   - Exit code: `0` (final run)
   - First meaningful error/warning:
     - initial attempt error before shim: `CS0246 VirtualButtonConfigSet could not be found` (InputManager)
     - final run first warning: `CS8765` nullability mismatch in `Direction.Equals`
   - Pass/fail: **PASS** (after minimal shim fix)
   - Output truncated: **No** (full command output captured to log)

2. Command:
   - `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-input-local-solution-build.log`
   - Exit code: `0`
   - First meaningful warning/error: none (build succeeded clean)
   - Pass/fail: **PASS**
   - Output truncated: **No**

3. Command:
   - `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **No**

4. Command:
   - `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **No**

5. Command:
   - `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: one test skipped (`StreamLiveness_DoesNotPruneWhenAccessUnknown`)
   - Pass/fail: **PASS**
   - Output truncated: **No**

6. Command:
   - `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **No**

7. Command:
   - `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **No**

## 7) Focused warning snapshot
From `/tmp/striv-input-local-build.log` filtered to `Stride.Input`:
- Focused warning line count: `164`
- Top codes:
  - `CS8618` (70)
  - `CS8602` (22)
  - `CS8601` (22)
  - `CS8604` (12)
  - `CS8600` (12)
- Next step recommendation on warnings: yes, proceed with a dedicated `Stride.Input` 5S lane (Sort/Set-in-order/Shine) rather than mixing cleanup into this migration step.

## 8) Deferred work
- Full `Stride.Input` 5S lane on the copied local tree.
- Targeted nullability warning cleanup.
- Windows RawInput runtime validation.
- SDL runtime smoke validation for Linux path.
- Review whether any currently excluded Windows input-source paths need re-introduction based on concrete runtime requirements.

## 9) Recommended next task
**Recommended next task:** `Stride.Input` 5S Sort, starting with warning inventory + ownership map for local copied files, then staged nullability/mechanical cleanup.
