# 1710 — M16e scene lifecycle transition proof

## 1) Files changed

- `striv/projects/StriV.Engine.Dominatus/Events/SceneLifecycleEvents.cs`
- `striv/projects/StriV.Engine.Dominatus/Actuators/ISceneLifecycleActuator.cs`
- `striv/projects/StriV.Engine.Dominatus/Nodes/SceneLifecycleNode.cs`
- `striv/projects/StriV.Engine.Dominatus/Transitions/SceneLifecycleTransition.cs` (new)
- `striv/tests/StriV.Engine.Dominatus.Tests/SceneLifecycleBridgeTests.cs` (new)

`Stride.Engine` production files changed: **none**.

## 2) Task scope

This change adds an explicit scene-membership lifecycle bridge proof:

- `EntitySceneAttachRequested -> ISceneLifecycleActuator.AttachEntityToSceneAsync -> EntitySceneAttached`
- `EntitySceneDetachRequested -> ISceneLifecycleActuator.DetachEntityFromSceneAsync -> EntitySceneDetached`

No runtime rewiring was performed.
No scene loading/activation migration was performed.

## 3) Current Stride behavior map

Observed from `Entity`, `Scene`, `SceneInstance`, and `SceneSystem`:

- `Entity.Scene` setter coordinates membership by removing from old scene entities and adding to new scene entities.
- If an entity has a transform parent, setting non-null scene throws; setting null scene first detaches transform parent.
- Scene entity collection insert sets internal `SceneValue`; remove nulls `SceneValue`.
- Root scene handling (`SceneInstance.RootScene`) is separate and recursively adds/removes entities/scenes into the manager.

Deferred:

- scene activation/loading (`SceneSystem` initial scene, `SceneInstance` root switching) and processor/runtime migration.

## 4) Bridge additions

- Added scene membership records:
  - `EntitySceneAttachRequested`, `EntitySceneAttached`
  - `EntitySceneDetachRequested`, `EntitySceneDetached`
- Extended `ISceneLifecycleActuator` with:
  - `AttachEntityToSceneAsync`
  - `DetachEntityFromSceneAsync`
- Added `SceneLifecycleTransition` helper for request->actuator->completed-event.
- Updated `SceneLifecycleNode` with request and execute helpers for attach/detach.

## 5) Tests

Added `SceneLifecycleBridgeTests` proving:

1. Attach transition invokes actuator, returns completed event, and reflects current Stride scene membership behavior.
2. Detach transition invokes actuator, returns completed event, and reflects current Stride detachment behavior.
3. Attach transition propagates actuator exceptions.
4. Attach/detach transitions reject null actuator.
5. Scene lifecycle records carry expected payload identities.
6. Scene lifecycle node request/execute helpers expose intended bridge surface.

No runtime rewiring is used; fake actuator is test-local only.

## 6) Legacy API containment

The fake actuator contains legacy null-as-detach inside a named compatibility boundary:

- `DetachFromScene(entity)` applies `entity.Scene = null!` with explicit comment.
- Null detach stays in test adapter boundary; transition surface remains explicit detach intent.

## 7) Behavior compatibility

- `Stride.Engine` behavior unchanged.
- No direct Dominatus dependency added to `Stride.Engine`.
- No runtime migration performed.

## 8) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | warnings in transitive legacy projects (e.g. `CS1030` in `Stride.Core`) | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16e-engine-focused.log` | 0 | existing `Stride.Engine` nullability warnings (e.g. `CS8767` in `AnimationChannel.cs`) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | existing analyzer/nullability warnings (e.g. `RS1036`, `CS1030`) | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | existing obsolete-test warnings (`CS0618`) | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | No |

## 9) Lessons / next strangler candidate (M16f)

Recommend M16f as one of:

1. deepen scene transition correlation (e.g. capture root-scene membership transition events distinct from scene loading);
2. introduce a test-only adapter implementation project for lifecycle actuators;
3. add a bounded processor lifecycle bridge proof;
4. bounded `EntityCloner` transition proof if processor scope should remain deferred.
