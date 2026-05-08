# M15d Stride.Engine Null-Assignment Root-Cause Repair

## 1. Files changed
- striv/projects/Stride.Engine/Animations/PlayingAnimation.cs
- striv/projects/Stride.Engine/Engine/Entity.cs
- striv/projects/Stride.Engine/Engine/EntityComponent.cs
- striv/projects/Stride.Engine/Engine/InstanceComponent.cs
- striv/projects/Stride.Engine/Engine/Scene.cs
- striv/projects/Stride.Engine/Engine/SceneInstance.cs
- striv/projects/Stride.Engine/Engine/SceneSystem.cs
- striv/projects/Stride.Engine/Engine/TransformComponent.cs
- striv/projects/Stride.Engine/Rendering/Compositing/SceneCameraSlotId.cs
- docs/stri-v/audits/1000+/1660-m15d-stride-engine-null-assignment-root-cause-repair.md

## 2. Task scope
M15d repaired M15c’s symptom-oriented nullable propagation by removing the widened contracts that caused reader-side warning spread. Goal was root-cause containment of null-assignment misuse, not adding more `?`/`!`.

## 3. M15c regression analysis
- M15b baseline: 964 focused warnings.
- M15c: 990 focused warnings (+26), with CS8602/CS8604 increases.
- Nullable widening in ownership/runtime-link members propagated nullability into many readers.
- M15d action: revert the M15c nullable widenings in the 9 source files to restore pre-propagation contracts and warning topology.

## 4. Root-cause pattern decisions
| File/member | Old null pattern | Root-cause replacement | Public nullable? | Rationale |
| --- | --- | --- | --- | --- |
| TransformComponent.parent/Parent | null used as detach sentinel | Reverted nullable widening; keep existing explicit hierarchy collection detach flow | No change | Prevent propagation while preserving legacy behavior |
| TransformComponent.TransformLink | null as optional link | Reverted widening in this pass | No change | Reader-side propagation outweighed benefit |
| Entity.TransformValue / EntityManager | null during detach/teardown | Reverted widening | No change | Restores focused-warning containment |
| Scene.parent | null for detach | Reverted widening | No change | Avoided broad reader impact |
| EntityComponent.Entity | null for detach | Reverted widening | No change | Avoided contract fan-out |
| SceneSystem.SceneInstance / GraphicsCompositor | null in teardown | Reverted widening | No change | Contain nullability spread from system access paths |
| SceneInstance.RootScene | null in teardown | Reverted widening | No change | Keeps existing API until explicit detach API is introduced safely |
| InstanceComponent.connectedInstancing | null for disconnect | Reverted widening | No change | Avoided extra flow warnings |
| PlayingAnimation.Evaluator/EndedTCS | null for disconnect | Reverted widening | No change | Prevented spread into animation readers |
| SceneCameraSlotId.AttachedCompositor | null for detach | Reverted widening | No change | Avoided cross-rendering nullable fallout |

## 5. Tests
No new tests added. This repair pass is a targeted rollback/containment change restoring known baseline behavior and warning profile.

## 6. Fixes applied
Per file, M15c nullable propagation was removed by restoring pre-M15c contracts. This is the minimal safe repair that directly addresses the observed regression and avoids further nullable fan-out.

## 7. After warnings
- Focused warnings after: **964**.
- Delta vs M15c regression baseline (990): **-26**.
- Warning distribution returned to M15b profile (e.g., CS8618 340, CS8625 144, CS8604 84, CS8602 82).
- Root-cause strategy result: containment succeeded by removing propagation.

## 8. Deferred
- Remaining Category 1/3 root-cause refactor (explicit detach/disconnect API migration) is still pending.
- Category 5 targets untouched (including UpdateEngine state-holder and scheduler/task state-machine nulls).
- Category 2 dispose/reset targets untouched.

## 9. Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0, focused warnings 990 before repair.
- same focused build command after repair => exit 0, focused warnings 964.
- `./striv/build/striv-check-focused-project.sh Stride.Engine` => exit 4 (focused gate still fails at 964 threshold policy).
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0.

## 10. Recommendation
Continue with explicit detach/disconnect refactor by subsystem (Entity/Transform/Scene first), but do it incrementally with tests and without public nullable contract widening.
