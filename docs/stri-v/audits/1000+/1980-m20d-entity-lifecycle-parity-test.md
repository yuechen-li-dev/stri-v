# 1980 — M20d entity lifecycle parity test

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleParityTests.cs`
- `docs/stri-v/audits/1000+/1980-m20d-entity-lifecycle-parity-test.md`

Production files changed: **none**.

## 2) Task scope
This milestone adds a parity test only.
- No runtime rewiring.
- No production migration/default path changes.
- No new engine features.

## 3) Legacy direct path
The direct helper uses current Stride lifecycle operations in this sequence:
1. `parent.Scene = scene`
2. `child.Scene = scene`
3. `child.Transform.Parent = parent.Transform`
4. `entityManager.Processors.Add(processor)`
5. `entityManager.AddEntityToProcessor(processor, child)`
6. `entityManager.RemoveEntityFromProcessor(processor, child)`
7. `entityManager.Processors.Remove(processor)`
8. `child.Transform.Parent = null` (legacy detach API)
9. `child.Scene = null` (legacy detach API)
10. `parent.Scene = null` (legacy detach API)

No Dominatus APIs are used in this helper.

## 4) Dominatus orchestrated path
The orchestrated helper uses the M20c engine-owned callsite:
- `EntityManager.RunEntityLifecycleFullCycleAsync(...)`

With:
- `DominatusEntityLifecycleOrchestrator` as the orchestrator implementation.
- Orchestrator delegates to `StriVEngineLifecycleRunner`, which runs runner/node/adapter lifecycle operations in Dominatus runtime.

## 5) Parity snapshot
Compared snapshot fields:
- `ParentSceneDetached`
- `ChildSceneDetached`
- `ChildTransformDetached`
- `ParentChildrenDoesNotContainChild`
- `ProcessorDetached`
- `ManagerDoesNotContainProcessor`
- `ProcessorAddedCount`
- `ProcessorRemovedCount`
- `AddedEntityName`
- `RemovedEntityName`

Why meaningful:
- They represent externally observable attach/cleanup lifecycle outcomes.
- They avoid cross-fixture object identity coupling (names/counts used for event targets rather than shared references).
- They avoid incidental internals (collection implementation order/details).

## 6) Test result
`EntityLifecycleParity_LegacyDirectAndDominatusOrchestratedFullCycle_ProduceSameSnapshot` passes.

Result: legacy and orchestrated snapshots match for the tested full-cycle path.

## 7) Dependency boundary verification
Command:
```bash
rg -n "StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true
```
Result: no matches.

## 8) Behavior compatibility
- Default engine behavior remains unchanged.
- No direct Dominatus dependency added to `Stride.Engine`.
- Orchestrated full-cycle remains opt-in via explicit callsite invocation.

## 9) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | Existing nullable/legacy warnings in transitive projects | Pass | Yes |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m20d-engine-focused.log` | 0 | Existing `CS8765` nullability warning in `Animations/CompressedTimeSpan.cs` | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | Existing `CS8618`/`CS0618` warnings in existing projects/tests | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | Existing upstream warnings during build/test | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | Existing upstream warnings during build/test | Pass | Yes |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | Existing `CS0618` warnings in tests | Pass | Yes |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | One existing skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | Yes |

## 10) Recommended next task
**M20e first production migration candidate using opt-in callsite**.

Reason: parity is now demonstrated for a concrete full lifecycle path without changing default behavior, so the next meaningful progression is a tightly scoped first migration candidate that explicitly opts in and can be measured against this parity baseline.
