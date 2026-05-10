# M22h AnimationLifecycle follow-up nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationUpdater.cs`
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationProcessor.cs`
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationClip.cs`
- `striv/tests/Stride.Engine.Tests/AnimationLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2200-m22h-animation-lifecycle-followup-cleanup.md`

## 2) Task scope
Folder-local follow-up cleanup in `AnimationLifecycle` only, plus tests/report. No animation system rewrite, no playback math/order/interpolation changes, and no Dominatus migration.

## 3) Before warnings
- Focused warning lines before: **684**
- AnimationLifecycle-focused warning lines were present across `AnimationUpdater`, `AnimationProcessor`, `AnimationClip`, `AnimationChannel`, evaluator groups, and curve contracts.
- Top relevant warning codes in focused build included: `CS8618`, `CS8625`, `CS8604`, `CS8602`, `CS8603`, `CS8600`, `CS8601`.

## 4) Animation lifecycle classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| AnimationUpdater.currentSourceChannels | CS8618 | field not initialized at construction | Updater should be inertly constructible and prepare binding on first update | constructor/default collection state | Initialize to empty list |
| AnimationClip.OptimizedAnimationDatas | CS8618 | optimization slot may be absent pre-optimize | Empty clip should be valid and non-throwing for default object lifecycle | collection slot semantics | Initialize to empty array |
| AnimationClip.Curves null slot writes in Optimize | CS8625/CS860x | list declared non-null elements but optimized path writes null markers | Null slot is intentional optimized marker after channel extraction | animation clip optional curve/channel slot | Make list element nullable (`AnimationCurve?`), add guarded access when non-null required |
| AnimationClip.GetCurve | CS8603 | returning null for missing/optimized channel | Missing channel and optimized channel are valid non-throwing query outcomes | generic curve/value contract | Return nullable `AnimationCurve?` |
| AnimationProcessor.AssociatedData fields | CS8618 | fields assigned in lifecycle hooks instead of constructor | AssociatedData exists pre-binding and is populated during processor lifecycle | associated data lifecycle | Mark lifecycle-bound fields nullable and guard at use sites |
| AnimationProcessor.CreatePushOperation | CS8604 | evaluator passed without static proof of non-null | Evaluator must exist before push operation in runtime path | runtime evaluator binding | Add explicit guard/throw for invariant violation |

## 5) Tests
Added tests in `AnimationLifecycleTests`:
- `AnimationUpdater_DefaultConstruction_HasValidInertState`
- `AnimationClip_DefaultConstruction_HasValidEmptyCollections`
- `AnimationProcessor_DefaultConstruction_DoesNotRequireRuntimeServices`

These tests pin intended construction/runtime-lifecycle contracts (inert construction, empty defaults, nullable-return behavior) rather than accidental legacy null/NRE quirks.

## 6) Fixes applied
- **AnimationUpdater**: initialized `currentSourceChannels` to `[]` so default construction is valid and first update handles compilation/rebind as designed.
- **AnimationClip**:
  - changed `Curves` to `List<AnimationCurve?>` to model optimized null slot semantics explicitly;
  - initialized `OptimizedAnimationDatas` to `[]`;
  - updated `GetCurve` return type to `AnimationCurve?`;
  - added guarded checks in `Optimize` for channel-to-curve mapping before using curve values.
- **AnimationProcessor**:
  - `AssociatedData` lifecycle-bound members marked nullable;
  - callsites now guard for unbound lifecycle state with explicit `InvalidOperationException`;
  - `CreatePushOperation` guards evaluator invariant before creating push operation;
  - `GetAnimationClipResult` now returns nullable result explicitly.

## 7) Deferred animation lifecycle issues
Deferred for future pass (needs broader runtime/lifecycle audit):
- evaluator-group internal optional state (`AnimationCurveEvaluator*` buckets)
- model/skeleton binding lifecycle interactions in evaluator/build paths
- playback task/evaluator lifecycle state-machine edges in blended multi-animation scheduling
- possible future split between clip data model and evaluator runtime binding state

## 8) After warnings
- Focused warning lines after: **670**
- AnimationLifecycle local delta: reduced warnings in targeted `AnimationUpdater`, `AnimationProcessor`, `AnimationClip` buckets.
- Total focused delta: **-14** (684 -> 670).

## 9) Next folder-local recommendation
Top next candidate: continue **AnimationLifecycle** on evaluator/binding buckets (`AnimationClipEvaluator`, `AnimationCurveEvaluatorOptimizedGroup`, `AnimationClipResult`, `AnimationData`) because:
- high local concentration remains,
- still lifecycle-local,
- medium risk with testability via construction/inert-state tests,
- meaningful additional warning reduction expected without touching playback math.

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0, warnings only, pass, output truncated in terminal capture: yes.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` => exit 0, pass, output truncated: yes.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` => exit 0, warnings only, pass, output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` => exit 0, pass, output truncated: no.
- Combined test chain:
  - `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`

  => exit 0, pass (with one expected skipped test in shader pipeline), output truncated: yes.
- `./striv/build/striv-build-core.sh` => exit 0, pass, output truncated: yes.
