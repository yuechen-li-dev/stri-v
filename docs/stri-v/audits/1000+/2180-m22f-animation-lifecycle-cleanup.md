# 2180 — M22f AnimationLifecycle folder-local nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/PlayingAnimation.cs`
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/ComputeBinaryCurve.cs`
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/ComputeCurveSampler.cs`
- `striv/tests/Stride.Engine.Tests/AnimationLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2180-m22f-animation-lifecycle-cleanup.md`

## 2) Task scope
Folder-local cleanup under `Engine/AnimationLifecycle` only. No animation system rewrite, no playback math/order changes, and no Dominatus migration.

## 3) Before warnings
- Focused warning lines before: **706**
- AnimationLifecycle warnings present across `PlayingAnimation`, `ComputeBinaryCurve`, `ComputeCurveSampler`, `AnimationProcessor`, `AnimationClip`, and related files.
- Top relevant warning codes included `CS8618`, `CS8625`, `CS8603`, `CS8602`, `CS8604`.

## 4) Classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
|---|---|---|---|---|---|
| `PlayingAnimation` fields/properties | CS8618 | Runtime-assigned members unset at default construction | Playing animation can exist in inert pre-bind state | runtime-assigned playback state | Made evaluator/TCS/name/clip nullable |
| `ComputeBinaryCurve` child nodes | CS8618 | Optional child nodes not set at construction | Node can evaluate with missing sides as default(T) | optional animation evaluator/task | Made left/right children nullable |
| `ComputeCurveSampler` curve slot | CS8618 | Curve set later by authoring/runtime | Sampler can evaluate default(T) without a curve | generic curve/value contract | Made backing field/property nullable |

## 5) Tests
Added `AnimationLifecycleTests`:
- `ComputeBinaryCurve_DefaultConstruction_AllowsMissingChildren`
- `ComputeCurveSampler_DefaultConstruction_WithoutCurve_EvaluatesDefaultValue`

These tests pin intended inert/default behavior, not accidental legacy null quirks.

## 6) Fixes applied
- `PlayingAnimation`: non-null runtime fields/properties moved to nullable contracts matching actual lifecycle.
- `ComputeBinaryCurve`: optional child references made nullable to match existing evaluate/update guards.
- `ComputeCurveSampler`: optional curve reference made nullable, preserving existing `BakeData`/`UpdateChanges` behavior.

## 7) Deferred animation lifecycle issues
Remaining local issues include:
- `AnimationUpdater.currentSourceChannels` construction-time state.
- `AnimationProcessor` associated-data and evaluator nullability in runtime binding path.
- `AnimationClip`/optimized data slot semantics and channel/model binding lifecycle.
- Potential future split between pure animation data model and runtime evaluator/playback state.

## 8) After warnings
- Focused warning lines after: **692**
- AnimationLifecycle-local delta from this pass: **-14** lines (net focused delta also **-14**)

## 9) Next folder-local recommendation
Based on current bucket density and locality: **`Engine/UpdaterReflection`** remains the strongest next target (high concentration, bounded subsystem, testable with reflection/update-engine focused tests).

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, warnings only, pass, output truncated: yes (captured to log).
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, warnings only, pass, output truncated: yes.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` → exit 0, warnings only, pass, output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` → exit 0, pass for all listed projects, output truncated: no.

Not run in this pass (time-bounded): remaining extended test commands from Part 8.
