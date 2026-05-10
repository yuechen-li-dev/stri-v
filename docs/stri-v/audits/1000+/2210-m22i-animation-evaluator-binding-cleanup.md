# 2210 - M22i Animation evaluator/binding follow-up cleanup

## 1) Files changed
- striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationClipEvaluator.cs
- striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationCurveEvaluatorOptimizedGroup.cs
- striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationClipResult.cs
- striv/projects/Stride.Engine/Engine/AnimationLifecycle/AnimationData.cs
- striv/tests/Stride.Engine.Tests/AnimationLifecycleTests.cs

## 2) Task scope
Folder-local AnimationLifecycle evaluator/binding cleanup only. No animation system rewrite, no interpolation/math changes, no ordering changes, and no Dominatus migration.

## 3) Before warnings
- Focused warning count before: **670**
- Evaluator/binding relevant lines before included:
  - AnimationClipEvaluator CS8602/CS8604/CS8601
  - AnimationCurveEvaluatorOptimizedGroup CS8618/CS8625/CS8601
  - AnimationClipResult CS8618
  - AnimationData CS8618
- Top focused warning codes before: CS8618 (200), CS8625 (96), CS8602 (68), CS8604 (66), CS8603 (48), CS8600 (48), CS8601 (42).

## 4) Animation evaluator classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| AnimationClipResult | CS8618 | collections/arrays uninitialized on default construction | default clip result should be inert and usable | channel/result collection default | initialize empty defaults |
| AnimationData | CS8618 | target keys/sorted arrays unset until build path | default data object should be valid empty model | nullable data slot semantics | initialize empty arrays |
| AnimationCurveEvaluatorOptimizedGroup | CS8618/CS8625 | animationData assigned null in cleanup | animationData is lifecycle-bound optional runtime binding | optional evaluator binding | mark nullable + guarded access |
| AnimationClipEvaluator | CS8602/CS8604/CS8601 | possible null curve from clip curve slots/index | curve may be absent in lifecycle state; evaluator should avoid unsafe access | evaluator cleanup/release state | guard index/null and treat as not-found channel |

## 5) Tests
Added/updated tests in `AnimationLifecycleTests`:
- `AnimationClipResult_DefaultConstruction_HasValidEmptyState`
- `AnimationData_DefaultConstruction_HasValidEmptyState`

Tests assert intended construction/inert behavior contracts, not legacy accidental null quirks.

## 6) Fixes applied
- AnimationClipResult: initialized `Channels`, `Data`, and `Objects` to empty defaults.
- AnimationData: initialized `TargetKeys`, `AnimationInitialValues`, `AnimationSortedValues` to empty defaults.
- AnimationCurveEvaluatorOptimizedGroup: made `animationData` nullable lifecycle-bound state and added explicit invalid-operation guards on active access.
- AnimationClipEvaluator: guarded clip curve index/null path before evaluator registration and downgraded missing curve to non-found channel factor path.

## 7) Deferred animation evaluator issues
- Runtime evaluator binding invariants across playback tasks.
- Model/skeleton lifecycle edges.
- Playback task lifecycle interactions.
- Math/interpolation behavior intentionally untouched.

## 8) After warnings
- Focused warning count after: **650**
- AnimationLifecycle evaluator/binding warnings reduced by removing:
  - AnimationData CS8618 bucket
  - AnimationClipResult CS8618 bucket
  - AnimationClipEvaluator CS8601/CS8602/CS8604 bucket
  - AnimationCurveEvaluatorOptimizedGroup CS8618/CS8625 bucket
- Total focused delta: **-20** (670 -> 650).

## 9) Next folder-local recommendation
From remaining buckets, best next local target is **AnimationLifecycle/AnimationChannel** then **AnimationLifecycle/AnimationProcessor**:
- high locality,
- still active warning density,
- testable without full runtime harness,
- lower risk than cross-folder rendering/entity refactors.

## 10) Validation results
See command runs in this session:
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` exit 0 (pass)
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` exit 0 (pass)
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` exit 0 (pass)
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` exit 0 (pass)
- all required test commands in the M22i validation block exit 0 (pass, with expected pre-existing warnings/skips)
- `./striv/build/striv-build-core.sh` exit 0 (pass)
