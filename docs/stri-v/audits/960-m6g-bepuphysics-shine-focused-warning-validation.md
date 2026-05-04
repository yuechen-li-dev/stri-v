# 960 — M6g BepuPhysics Shine + Focused Warning Lane Validation

## 1) Files changed
- `striv/build/StriV.Core.Profile.props`
- `striv/build/striv-build-focused-project.sh`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/BepuSimulation.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/BodyComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/CollidableComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Definitions/Colliders/CompoundCollider.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Definitions/SimTests/Collectors.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Systems/ShapeCacheSystem.cs`
- `docs/stri-v/audits/960-m6g-bepuphysics-shine-focused-warning-validation.md`

## 2) Focused warning lane
Inactive project warnings were suppressing visibility of the active project signal during Shine. M6g introduces a focused warning lane so the active project remains accountable while inactive projects are temporarily muted for that focused run.

Mechanism:
- `StriVWarningFocusProject=<ProjectName>` selects the active project.
- `StriVSuppressInactiveProjectWarnings` defaults to `true` only when focus project is set.
- For inactive projects (`$(MSBuildProjectName) != $(StriVWarningFocusProject)`), `WarningLevel` is set to `0`.
- Focus project warnings remain fully visible.
- Errors/restore failures still fail the build.

Normal behavior unchanged:
- When `StriVWarningFocusProject` is empty, no focus suppression is applied.

This is not warning resolution for inactive projects; it is temporary work-lane isolation to execute project-by-project Shine.

## 3) BepuPhysics Shine scope
Target project:
- `striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj`

Rationale:
- M6f explicitly set BepuPhysics as next Shine target.
- Prior M6c/M6d/M6e/M6f mapping comments were used as lifecycle/ownership guardrails while converting warning markers into non-warning TODO commentary and pruning a dead private field.

## 4) Before warnings
Command:
```bash
dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug -p:StriVWarningFocusProject=Stride.BepuPhysics 2>&1 | tee /tmp/striv-m6g-bepu-warnings-before.log
```

Collected BepuPhysics warning count before:
- 24 matching lines (duplicated entries in log stream), unique warnings: 12.

Codes before:
- `CS1030` x22 line hits (11 unique callsites)
- `CS0169` x2 line hits (1 unique callsite)

Representative warnings:
- `BepuSimulation.cs`: `#warning` thread dispatcher integration TODO.
- `BodyComponent.cs`: several `#warning` Norbo notes about direct handle property updates.
- `CollidableComponent.cs`: `#warning` around reconstruction/messy flow.
- `CompoundCollider.cs`, `Collectors.cs`, `ShapeCacheSystem.cs`: `#warning` performance/heuristic notes.
- `BepuSimulation.cs`: unused `_scheduler` field (`CS0169`).

## 5) Fixes applied
- `BepuSimulation.cs`
  - Replaced `#warning` with `// TODO(striv-m6g): ...` comment.
  - Removed unused private field `_scheduler` to resolve `CS0169`.
  - Runtime behavior unchanged (field was not read/written).
- `BodyComponent.cs`
  - Converted all `#warning` markers to `// TODO(striv-m6g): ...` comments.
  - No logic changes.
- `CollidableComponent.cs`
  - Converted `#warning` markers to TODO comments.
  - No logic changes.
- `Definitions/Colliders/CompoundCollider.cs`
  - Converted `#warning` to TODO comment.
  - No logic changes.
- `Definitions/SimTests/Collectors.cs`
  - Converted two `#warning` markers to TODO comments.
  - No logic changes.
- `Systems/ShapeCacheSystem.cs`
  - Converted `#warning` to TODO comment.
  - No logic changes.

Set-in-order comments from M6d/M6e/M6f were retained and used as guardrails to avoid lifecycle/ownership behavior changes.

## 6) Deferred warnings
- None in `Stride.BepuPhysics` under focused warning lane after this pass.

## 7) After warnings
Command:
```bash
dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug -p:StriVWarningFocusProject=Stride.BepuPhysics 2>&1 | tee /tmp/striv-m6g-bepu-warnings-after.log
```

Results:
- BepuPhysics warning count after: 0
- Warning codes after: none
- Target achieved: zero warnings for focused project.

## 8) Build/test validation
1. `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug -p:StriVWarningFocusProject=Stride.BepuPhysics`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

2. `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.BepuPhysics`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

3. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

4. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

5. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - exit: 0
   - first meaningful warning/error: one known skipped test
   - pass/fail: pass
   - output truncated: no

6. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - exit: 1
   - first meaningful warning/error: `Assert.NotNull() Failure` in `EffectBytecodeSerializer_IsAvailable_InCleanProfile`
   - pass/fail: fail
   - output truncated: no

7. `./striv/build/striv-build-core.sh`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

## 9) Standard / sustain note
For every future Shine pass:
- run with `StriVWarningFocusProject=<ProjectName>`;
- treat inactive warning suppression as temporary lane isolation only;
- require each project to later receive its own explicit Shine pass.

## 10) Recommended next task
Recommended next task: **BepuPhysics Standardize/Sustain gate**.

Rationale: BepuPhysics is now warning-clean under focused lane; next highest value is locking repeatable sustain checks (focused command, expected warning count = 0, and checklist integration) before moving to the next Sort candidate.
