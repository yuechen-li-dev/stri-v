# 1700 — M16d Transform Lifecycle Transition Proof

## 1) Files changed

Bridge/test files changed:

- `striv/projects/StriV.Engine.Dominatus/Transitions/TransformLifecycleTransition.cs` (new)
- `striv/projects/StriV.Engine.Dominatus/Nodes/EntityAttachmentNode.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/TransformLifecycleBridgeTests.cs`

No `striv/projects/Stride.Engine/*` production file changes were made.

## 2) Task scope

This increment proves an explicit bridge transition shape for transform parent lifecycle only:

- `TransformParentAttachRequested -> actuator.AttachParentAsync -> TransformParentAttached`
- `TransformParentDetachRequested -> actuator.DetachParentAsync -> TransformParentDetached`

Scope is intentionally limited to bridge/test proof. No runtime rewiring, no Dominatus dependency added to `Stride.Engine`, and no migration of `SceneSystem`, `EntityManager`, processors, or `TransformComponent`.

## 3) Transition design

`TransformLifecycleTransition` is a small bridge-level helper with two async methods:

- `AttachParentAsync(request, actuator, ct)`
  - validates actuator non-null
  - delegates mutation to `ITransformLifecycleActuator.AttachParentAsync`
  - returns `TransformParentAttached` with the same child/parent references from request
- `DetachParentAsync(request, actuator, ct)`
  - validates actuator non-null
  - delegates mutation to `ITransformLifecycleActuator.DetachParentAsync`
  - returns `TransformParentDetached` with the same child reference from request

Failure behavior: exceptions from actuator are intentionally propagated; transition helper does not swallow them.

`EntityAttachmentNode` now includes optional execution helpers that forward requests to `TransformLifecycleTransition`, keeping bridge surface aligned without introducing runtime node runner rewiring.

## 4) Tests

Added/extended bridge tests in `TransformLifecycleBridgeTests`:

1. `TransformLifecycleTransition_AttachParent_InvokesActuatorAndReturnsAttachedEvent`
   - proves attach request->actuator->completed event correlation
   - verifies returned event identity, parent/children synchronization, and attach call count

2. `TransformLifecycleTransition_DetachParent_InvokesActuatorAndReturnsDetachedEvent`
   - proves detach request->actuator->completed event correlation
   - verifies returned event identity, detach state, parent child-list update, and detach call count

3. `TransformLifecycleTransition_AttachParent_PropagatesActuatorFailure`
   - uses throwing fake actuator
   - verifies exception propagation (no completed event is synthesized on failure)

4. `TransformLifecycleTransition_RejectsNullActuator`
   - verifies argument validation for both attach/detach transitions

Existing M16c tests remain and continue to prove bridge event payload identity and fake-actuator compatibility with current legacy parent API.

No engine runtime rewiring was introduced.

## 5) Legacy API containment

Test-local fake actuator detachment continues to wrap legacy behavior (`child.Transform.Parent = null`) via a dedicated helper method:

- `DetachFromParent(Entity child)` uses `child.Transform.Parent = null!;`
- the null-for-detach contract is now explicitly documented in that boundary comment

This keeps nullable warning suppression constrained to the test fake compatibility layer, and preserves a clean seam for future production adapter replacement.

## 6) Behavior compatibility

- `Stride.Engine` behavior remains unchanged.
- No direct Dominatus dependency was added to `Stride.Engine`.
- No runtime migration was introduced in M16d.

## 7) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated? |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | Existing upstream warnings in transitively built Stride projects (e.g. `Stride.Core` nullability/obsolete warnings) | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16d-engine-focused.log` | 0 | Existing Engine warning baseline (e.g. `CS8767` in `AnimationChannel.cs`) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | Existing warning baseline in Stride/Stride.Reflection test projects | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | One known skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | Yes |

## 8) Lessons / next strangler candidate (M16e)

Recommended next bounded step: introduce the same request -> actuator -> completed-event bridge proof for **scene attach/detach** (or scene activation lifecycle), still outside `Stride.Engine` runtime rewiring.

Alternative bounded path: create a test-only production-adapter prototype project that implements bridge actuators against current Stride APIs, still isolated from core runtime migration.
