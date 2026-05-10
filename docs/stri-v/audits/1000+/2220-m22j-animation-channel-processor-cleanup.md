# 2220 — M22j AnimationChannel/AnimationProcessor cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationChannel.cs`
- `striv/tests/Stride.Engine.Tests/AnimationLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2220-m22j-animation-channel-processor-cleanup.md`

## 2) Task scope
This pass stayed folder-local to `AnimationLifecycle` with focus on `AnimationChannel` and `AnimationProcessor` warning clusters, plus local tests/report updates. No animation playback rewrite, no interpolation/evaluator math change, no animation ordering change, and no Dominatus migration.

## 3) Before warnings
- Focused warning count before: **650** (`/tmp/striv-m22j-engine-warning-lines-before.log`).
- Relevant warning lines before:
  - `AnimationChannel`: CS8618 (`TargetObject`, `TargetProperty`, evaluator `currentKeyFrame`), CS8767 (`ErrorComparer.Compare`), CS8602 (linked-list next dereference).
  - `AnimationProcessor`: CS8601 (`ref associatedData.AnimationClipResult`), CS8602 (`playingAnimation.Clip.Duration` in PlayOnce branch).
- Top relevant codes in focused baseline: CS8618, CS8602, CS8601, CS8767.

## 4) Animation channel/processor classification table

| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --------- | ------- | ------- | ----------------- | -------- | ------ |
| AnimationChannel.TargetObject/TargetProperty | CS8618 | non-nullable metadata unset at default construction | channel can be unbound authoring/runtime metadata until linked | optional channel/curve target | Made both properties nullable (`string?`) and added tests for inert unbound state |
| AnimationChannel.ErrorComparer.Compare | CS8767 | comparer interface expects nullable args | comparer should tolerate null comparer inputs deterministically | collection/default state | Updated signature to nullable and added deterministic null ordering |
| AnimationChannel.Evaluator.currentKeyFrame | CS8618 | field assigned through initialization flow, not ctor flow | evaluator enumerator is runtime-initialized; using before init is invalid | runtime invariant not visible to compiler | made field nullable + explicit InvalidOperationException guard in SetTime |
| AnimationChannel linked-list next access in Fitting | CS8602 | compiler cannot prove `Next` non-null after queue/update flow | next frame should exist for actively processed segment | needs animation runtime audit | **Deferred** (kept behavior unchanged due risk in fitting loop semantics) |
| AnimationProcessor.Blender.Compute result ref | CS8601 | nullable `AnimationClipResult` ref assigned by runtime blender | uninitialized clip result is valid before first compute | associated data lifecycle | **Deferred** (requires broader contract refactor between blender/result ownership) |
| AnimationProcessor.PlayOnce clip dereference | CS8602 | clip nullability not carried across branches | clip is expected for enabled playing entries; guard needs careful ordering audit | playback state/task lifecycle | **Deferred** (left as-is; avoid changing playback ordering/rules here) |

## 5) Tests
Added tests in `AnimationLifecycleTests.cs`:
- `AnimationChannel_DefaultConstruction_HasValidInertState`
- `AnimationChannel_MissingTarget_IsAllowedAsUnboundAuthoringState`

These pin intended behavior (default-inert and valid unbound metadata), not accidental NRE/null quirks.

## 6) Fixes applied
### AnimationChannel.cs
- `TargetObject`/`TargetProperty`: `string` → `string?` to encode optional unbound channel target metadata.
- `ErrorComparer.Compare`: updated comparer signature to nullable inputs and explicit null handling.
- Evaluator lifecycle: `currentKeyFrame` is nullable and guarded in `SetTime` with deterministic `InvalidOperationException` if used before initialization.

Behavior intent: preserve existing animation runtime behavior while improving nullable contracts for valid authoring/runtime states.

## 7) Deferred animation processor issues
Still deferred in this pass:
- runtime evaluator binding invariants,
- model/skeleton lifecycle edges,
- playback task lifecycle interactions,
- math/interpolation behavior untouched.

## 8) After warnings
- Focused warning count after: **640** (`/tmp/striv-m22j-engine-warning-lines-after.log`).
- AnimationLifecycle local delta from this pass:
  - `AnimationChannel` CS8618 + CS8767 warnings removed.
  - `AnimationChannel` CS8602 remains.
  - `AnimationProcessor` warnings unchanged (CS8601, CS8602).
- Total focused delta: **-10** warnings.

## 9) Next folder-local recommendation
Based on post-pass buckets, next local target should remain in `AnimationLifecycle` for low-risk continuity:
1. `AnimationLifecycle/AnimationProcessor.cs` (2 warnings, lifecycle contracts already scoped in this pass).
2. `AnimationLifecycle/AnimationBlender.cs` (CS8602/CS8625 pair, likely adjacent lifecycle-result ownership).
3. `AnimationLifecycle/AnimationKeyValuePairArraySerializer.cs` (constructor lifecycle + obsolete API warning).

Rationale: highest chance of additional reduction with minimal cross-folder behavior risk.

## 10) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` — exit **0**, pass, output truncated: **yes**.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` — exit **0**, pass, output truncated: **yes**.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` — exit **0**, pass, first warning: existing baseline warnings in other projects, output truncated: **yes**.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` — exit **0**, pass.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` — exit **0**, pass.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` — exit **0**, pass.
- `./striv/build/striv-build-core.sh` — exit **0**, pass.
