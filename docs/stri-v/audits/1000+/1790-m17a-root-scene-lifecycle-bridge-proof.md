# 1790 — M17a root-scene lifecycle bridge proof

## 1) Files changed
- Bridge:
  - `striv/projects/StriV.Engine.Dominatus/Events/SceneLifecycleEvents.cs`
  - `striv/projects/StriV.Engine.Dominatus/Actuators/ISceneLifecycleActuator.cs`
  - `striv/projects/StriV.Engine.Dominatus/Transitions/SceneLifecycleTransition.cs`
  - `striv/projects/StriV.Engine.Dominatus/Nodes/SceneLifecycleNode.cs`
- Adapter:
  - `striv/projects/StriV.Engine.Dominatus.Adapters/Scene/StrideSceneLifecycleActuator.cs`
- Tests:
  - `striv/tests/StriV.Engine.Dominatus.Tests/RootSceneLifecycleBridgeTests.cs`
  - `striv/tests/StriV.Engine.Dominatus.Tests/Adapters/StrideSceneLifecycleTestAdapter.cs`
  - `striv/tests/StriV.Engine.Dominatus.Tests/SceneLifecycleBridgeTests.cs` (interface conformance updates)

`Stride.Engine` production files changed: **none**.

## 2) Task scope
Implemented a bounded bridge proof for root-scene set/clear lifecycle with transition + actuator + production adapter + tests.
No scene loading/activation migration. No SceneSystem rewiring. No runtime opt-in/default behavior changes.

## 3) Current behavior map
- `SceneInstance.RootScene` setter:
  - removes old root scene recursively from manager and renderer types;
  - sets backing field;
  - adds new root scene recursively and hooks renderer types;
  - raises `RootSceneChanged`.
- Clear behavior is `RootScene = null`.
- Add/remove is recursive across child scenes and entities, and manager entity membership is updated.
- This is distinct from plain scene membership (`entity.Scene = ...`) because root scene affects the `SceneInstance` manager graph.
- Deferred to later slices: scene loading (`InitialSceneUrl`, async content loading), scene activation timing in `SceneSystem`, script/runtime scheduler interactions.

## 4) Bridge additions
- Events/messages:
  - `RootSceneSetRequested`, `RootSceneSet`, `RootSceneClearRequested`, `RootSceneCleared`.
- Actuator methods:
  - `SetRootSceneAsync(...)`, `ClearRootSceneAsync(...)`.
- Transition helpers:
  - `SceneLifecycleTransition.SetRootSceneAsync(...)`, `ClearRootSceneAsync(...)`.
- Node updates:
  - `RequestRootSceneSet`, `RequestRootSceneClear`, `ExecuteRootSceneSetAsync`, `ExecuteRootSceneClearAsync`.

Naming used **Set/Clear** to match `SceneInstance.RootScene` property semantics.

## 5) Adapter behavior
`StrideSceneLifecycleActuator` now sets/clears `SceneInstance.RootScene` directly.
Root clear uses explicit `null` assignment encapsulated behind adapter helper boundary (`ClearRootScene`) to contain legacy null-clear semantics.

## 6) Tests
Added root-scene bridge tests proving:
- set transition invokes production adapter and returns completed event;
- clear transition invokes production adapter and returns completed event;
- actuator failures propagate and suppress completed event path;
- null actuator guard rails;
- node surface exposes root-scene request/execute intent;
- observed manager side effects: entities become managed on set and unmanaged on clear.

## 7) Behavior compatibility
- No engine behavior change.
- No direct Dominatus dependency added to `Stride.Engine`.
- No runtime migration/rewire.

## 8) Validation results
(Each command exit code: 0)
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - First meaningful warning: baseline legacy warnings in `Stride.Core` (nullability/#warning); also one transient file-lock retry warning when run in parallel.
  - Pass; output truncated: yes.
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
  - First meaningful warning: baseline warnings + transient copy retry warning under parallel build.
  - Pass; output truncated: yes.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - 46 passed.
  - Pass; output truncated: yes.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m17a-engine-focused.log`
  - First meaningful warning: baseline nullability warnings.
  - Pass; output truncated: yes.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - First meaningful warning: baseline assembly processor/project warnings.
  - Pass; output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - All listed projects pass, 0 warnings in focused mode.
  - Pass; output truncated: no.
- `dotnet test ...` suite batch per requested commands (Stride.Engine.Tests, StriV.Engine.Dominatus.Tests, Stride.Core.Reflection.Tests, Stride.Games.Tests, Stride.Input.Tests, StriV.CleanGraph.Tests, StriV.AssetTool.Tests, StriV.AssetPipeline.Tests --no-build, StriV.ShaderPipeline.Tests --no-build)
  - All pass; one intentionally skipped shader test.
  - Pass; output truncated: yes.
- `./striv/build/striv-build-core.sh`
  - Full script pass.
  - Pass; output truncated: no.

## 9) Recommended next task
**Root scene composition with scene membership/processor lifecycle** (bounded): add composition-oriented tests that prove ordering and invariants when root scene set/clear co-occurs with entity scene membership and processor registration transitions.
