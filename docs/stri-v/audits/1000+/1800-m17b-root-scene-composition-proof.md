# 1800 — M17b Root-Scene Composition Proof

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Integration/EngineLifecycleCompositionTests.cs`
- `docs/stri-v/audits/1000+/1800-m17b-root-scene-composition-proof.md`

Production files changed: **No**.

## 2) Task scope
This slice proves composition across existing Dominatus bridge transitions and production adapters for:
- root-scene lifecycle (`SetRootSceneAsync` / `ClearRootSceneAsync`),
- processor system lifecycle (`AddProcessorToSystemAsync`), and
- processor entity lifecycle (`AddEntityToProcessorAsync`).

No runtime rewiring, no SceneSystem migration, no scheduler/script-system migration, and no warning cleanup changes were introduced.

## 3) Composition flow proven
Tested sequence:
1. `RootSceneSetRequested` -> `SceneLifecycleTransition.SetRootSceneAsync(...)` -> `RootSceneSet`
2. `ProcessorSystemAddRequested` -> `ProcessorLifecycleTransition.AddProcessorToSystemAsync(...)` -> `ProcessorSystemAdded`
3. `ProcessorEntityAddRequested` -> `ProcessorLifecycleTransition.AddEntityToProcessorAsync(...)` -> `ProcessorEntityAdded`
4. `RootSceneClearRequested` -> `SceneLifecycleTransition.ClearRootSceneAsync(...)` -> `RootSceneCleared`

## 4) Tests
### Added integration test
`RootSceneComposition_SetRootScene_ThenProcessorTransitions_ComposeThroughProductionAdapters`

Assertions include:
- completed event identity checks at each transition stage;
- root-scene set state (`sceneInstance.RootScene`) and manager graph membership (`entity.EntityManager`);
- processor registration state (`processor.EntityManager`);
- processor callback evidence for explicit entity add (`AddedCount == 1`, identity match);
- root-scene clear state (`sceneInstance.RootScene == null`) and entity un-managed state (`entity.EntityManager == null`).

Adapters used in the test:
- `StrideSceneLifecycleActuator`
- `StrideProcessorLifecycleActuator`

Transitions used in the test:
- `SceneLifecycleTransition.SetRootSceneAsync`
- `ProcessorLifecycleTransition.AddProcessorToSystemAsync`
- `ProcessorLifecycleTransition.AddEntityToProcessorAsync`
- `SceneLifecycleTransition.ClearRootSceneAsync`

## 5) Observed lifecycle policy
- Root-scene set updates SceneInstance manager graph membership (entity becomes managed by the scene instance).
- Processor entity membership is explicitly actuated in this proof (`AddEntityToProcessorAsync`) rather than assumed implicit.
- Root-scene clear unmanages entities via current Stride root-scene clear path.
- This proof does not impose or assume automatic processor-entity remove on root-scene clear.

## 6) Behavior compatibility
- No engine runtime behavior changes.
- No direct Dominatus dependency added to `Stride.Engine`.
- Adapters remain opt-in through bridge transitions.

## 7) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | existing Stride warning set during transitive build (e.g., `CS1030 PERF: Do not copy byte-for-byte`) | Pass | Yes |
| `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m17b-engine-focused.log` | 0 | existing engine warnings (e.g., `CS8765` nullability mismatch) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | existing warning set (e.g., `CS1030`, `RS1036`) | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | Pass | No |
| `dotnet test ...` suite chain across requested test projects | 0 | one expected skip in `StriV.ShaderPipeline.Tests` (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | Yes |

## 8) Recommended next task
**Root-scene composition remove/cleanup expansion**:
- Add a bounded follow-up proof for explicit `ProcessorEntityRemoveRequested` + `ProcessorSystemRemoveRequested` composed with root-scene clear,
- Keep it adapter/transition only,
- Pin ordering policy without changing default runtime behavior.
