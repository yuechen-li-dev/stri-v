# M15c Stride.Engine Category 1/3 Null Contract Cleanup

## 1) Files changed
- striv/projects/Stride.Engine/Engine/TransformComponent.cs
- striv/projects/Stride.Engine/Engine/Entity.cs
- striv/projects/Stride.Engine/Engine/Scene.cs
- striv/projects/Stride.Engine/Engine/EntityComponent.cs
- striv/projects/Stride.Engine/Engine/SceneSystem.cs
- striv/projects/Stride.Engine/Engine/SceneInstance.cs
- striv/projects/Stride.Engine/Engine/InstanceComponent.cs
- striv/projects/Stride.Engine/Animations/PlayingAnimation.cs
- striv/projects/Stride.Engine/Rendering/Compositing/SceneCameraSlotId.cs

## 2) Task scope
- Focused on Category 1 (detach relationship nulls) and Category 3 (optional runtime links).
- No Category 4 local placeholder mop-up performed.
- Category 5 lifecycle/scheduler sites were not edited.

## 3) Before warnings
- Focused warning count before: **964**.
- Top code distribution before: CS8618 340, CS8625 144, CS8604 84, CS8602 82, CS8600 70.
- Top buckets before included: `Engine/SceneInstance.cs CS8622` (22), `Engine/SceneSystem.cs CS8618` (18), `Engine/Processors/CameraProcessor.cs CS8625` (12), `Engine/EntityComponentCollection.cs CS8625` (12), `Engine/EntityManager.cs CS8625` (10).

## 4) Target classification table
| File/site | Category | Meaning | Fix | Status |
| --- | --- | --- | --- | --- |
| TransformComponent parent + Parent | 1 | hierarchy detach sentinel | `TransformComponent?` for parent storage/property | Applied |
| TransformComponent TransformLink | 3 | runtime link can be disconnected | `TransformLink?` | Applied |
| Entity EntityManager | 1 | detached entity can have no manager | `EntityManager?` | Applied |
| Entity TransformValue | 1 | component detach can clear transform backing ref | `TransformComponent?` backing + `Transform => TransformValue!` | Applied |
| Scene parent + Parent | 1 | scene parent detach | `Scene?` storage/property | Applied |
| EntityComponent Entity | 1 | component detach from entity | `Entity?` | Applied |
| SceneSystem SceneInstance | 1 | system teardown nulls instance | `SceneInstance?` | Applied |
| SceneSystem GraphicsCompositor | 1/3 | teardown + optional connection | `GraphicsCompositor?` | Applied |
| SceneInstance RootScene | 1 | teardown sets root to null | `Scene?` backing/property + ctor param `Scene?` | Applied |
| InstanceComponent connectedInstancing | 3 | optional runtime connection | `InstancingEntityTransform?` | Applied |
| PlayingAnimation Evaluator/EndedTCS | 3 | optional evaluator/task link | nullable fields | Applied |
| SceneCameraSlotId.AttachedCompositor | 3 | optional compositor link | nullable field | Applied |

## 5) Fixes applied
Changes were contract-only nullability adjustments aligned with existing runtime behavior where these references were already set to null in detach/teardown paths.

## 6) Tests
No new tests added. This pass intentionally constrained to contract/nullability declarations with no intended behavior changes.

## 7) After warnings
- Focused warning count after: **990**.
- Delta vs M15b baseline (964): **+26** (regression).
- Top code distribution after: CS8618 316, CS8602 150, CS8604 108, CS8625 98, CS8603 74.
- Category 1/3 cleanup appears to have exposed additional downstream nullable flow warnings beyond direct assignment sites.

## 8) Deferred targets
- Several Category 1/3 callsites now show new CS8602/CS8604 chains and need localized guard refinement.
- Category 5 explicitly untouched (e.g., `UpdateEngine.cs:74` remains unchanged).
- Category 2 dispose/reset patterns untouched.

## 9) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0, warnings present, not truncated in log file.
- `./striv/build/striv-check-focused-project.sh Stride.Engine` => exit 4, first meaningful failure: focused warning gate failed at count 990.

## 10) Recommendation
Fix blocker: perform a focused follow-up for the newly surfaced reader-side null-flow warnings in Entity/Scene/Camera/Transform subsystems before broader Category 1/3 expansion.
