# M16c — Transform Lifecycle Dominatus Strangler Proof

## 1) Files changed

Bridge/test files changed:
- `striv/projects/StriV.Engine.Dominatus/Events/TransformLifecycleEvents.cs` (new)
- `striv/projects/StriV.Engine.Dominatus/Actuators/ITransformLifecycleActuator.cs` (new)
- `striv/projects/StriV.Engine.Dominatus/Nodes/EntityAttachmentNode.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/BridgeSurfaceTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/TransformLifecycleBridgeTests.cs` (new)

Production engine files changed:
- None.

## 2) Task scope

This change is the first strangler proof only. It models transform-parent attach/detach intent in the StriV Dominatus bridge surface while preserving existing Stride behavior.

It does **not**:
- migrate SceneSystem or EntityManager runtime flow;
- rewire engine runtime dispatch;
- replace attach/detach implementation;
- introduce Dominatus dependencies into `Stride.Engine`.

## 3) Current Stride behavior map

From inspection of `TransformComponent`, `Entity`, and related manager flow:
- `TransformComponent.Parent` setter performs add/remove through parent `Children` collections.
- Parent change updates child collections and internally mutates `item.parent` during collection hooks.
- Hierarchy-change notifications are emitted through `EntityManager.OnHierarchyChanged` and transform processor child-collection notifications when manager exists.
- If entity has scene and receives a parent, `Entity.Scene` is set to null in setter path first.
- `Entity.Scene` setter uses null assignment to detach from parent (`Transform.Parent = null`) when currently parented.
- Null parent assignment is current detach semantic for transform parenting.
- No explicit domain events/callback API exists on `TransformComponent` for attach/detach intent; behavior is implicit in setter/collection side effects.

Compatibility constraints preserved in M16c:
- `child.Transform.Parent = parent.Transform` attach behavior unchanged.
- `child.Transform.Parent = null` detach behavior unchanged.
- Children collection synchronization behavior unchanged.
- No public API changes in `Stride.Engine`.

## 4) Bridge additions

Added transform lifecycle bridge surface:
- Events/messages:
  - `TransformParentAttachRequested`
  - `TransformParentAttached`
  - `TransformParentDetachRequested`
  - `TransformParentDetached`
- Actuator contract:
  - `ITransformLifecycleActuator` with attach/detach async methods.
- Node skeleton update:
  - `EntityAttachmentNode.RequestTransformAttach(...)`
  - `EntityAttachmentNode.RequestTransformDetach(...)`

No blackboard keys added in M16c.

## 5) Tests

Added deterministic bridge tests in `TransformLifecycleBridgeTests`:
1. `TransformLifecycleActuator_AttachParent_UsesExistingStrideParenting`
   - Uses fake actuator (`child.Transform.Parent = parent.Transform`).
   - Proves parent and children collection reflect attach.
2. `TransformLifecycleActuator_DetachParent_UsesExistingStrideDetach`
   - Uses fake actuator (`child.Transform.Parent = null`).
   - Proves parent cleared and child removed from old parent children collection.
3. `TransformLifecycleEvents_CarryExpectedEntities`
   - Proves event payload identity.
4. `EntityAttachmentNode_Surface_ExposesTransformLifecycleIntent`
   - Proves minimal node skeleton APIs exist and produce expected records.

Also updated `BridgeSurfaceTests` to include transform event and node method surface checks.

No runtime rewiring; tests operate entirely at bridge surface and fake actuator layer.

## 6) Behavior compatibility

- `Stride.Engine` lifecycle behavior unchanged.
- Fake actuator is test-local and intentionally wraps existing null-assignment semantics.
- No direct Dominatus dependency added to `Stride.Engine`.

## 7) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | Existing warning stream from transitive Stride projects (no new errors) | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | `CS8625` in test fake-detach null assignment (expected legacy API usage in fake) | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16c-engine-focused.log` | 0 | Existing Stride warning baseline (nullable/legacy warnings) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | Existing warning baseline in unrelated projects | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | None (all listed as pass, warning count 0) | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | `CS8625` in fake detach test | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | Existing obsolete-test warnings (`OldCollectionDescriptor`) | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | One pre-existing skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | Yes |

## 8) Lessons / next strangler candidate (M16d)

Recommended next step for M16d:
1. Keep deepening transform attach/detach bridge (request -> actuator -> completed event correlation).
2. Add scene attach/detach bridge events for root-scene transitions while still avoiding runtime rewiring.
3. Introduce an adapter implementation project (outside `Stride.Engine`) that can be opted into by integration tests.
4. Alternative small bounded target: `EntityCloner` lifecycle boundary if scene transitions become too broad for next increment.

Convergence state: **Success** for M16c scope.
