# 1730 — M16h production lifecycle adapters validation

## 1) Files changed
- Added project: `striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj`
- Added docs: `striv/projects/StriV.Engine.Dominatus.Adapters/README.md`
- Added adapters:
  - `striv/projects/StriV.Engine.Dominatus.Adapters/Transform/StrideTransformLifecycleActuator.cs`
  - `striv/projects/StriV.Engine.Dominatus.Adapters/Scene/StrideSceneLifecycleActuator.cs`
- Added tests: `striv/tests/StriV.Engine.Dominatus.Tests/ProductionAdapterTests.cs`
- Updated test references: `striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj`
- Updated solution membership: `striv/StriV.Core.slnx`

`Stride.Engine` production files changed: **none**.

## 2) Task scope
This task created a narrow production-side adapter project outside `Stride.Engine`, implemented only transform + scene entity attach/detach lifecycle actuators, and validated behavior with targeted tests. No runtime rewiring or behavior migration was performed.

## 3) Architecture boundary
Dependency direction now remains:
- `Stride.Engine` has no Dominatus reference.
- `StriV.Engine.Dominatus.Adapters` references:
  - `Stride.Engine`
  - `StriV.Engine.Dominatus`
- Adapter classes implement Dominatus actuator interfaces and call existing legacy Stride APIs.

## 4) Adapter implementation
- `StrideTransformLifecycleActuator`
  - attach: `child.Transform.Parent = parent.Transform`
  - detach helper contains legacy null detach boundary with explicit comment.
- `StrideSceneLifecycleActuator`
  - attach entity: `entity.Scene = scene`
  - detach helper contains legacy null detach boundary with explicit comment.
  - `AttachSceneAsync` / `DetachSceneAsync` implemented as minimal supported no-op boundaries with null guard.

## 5) Tests
Added production adapter tests proving:
- Transform attach/detach semantics match current Stride behavior.
- Scene attach/detach semantics match current Stride behavior.
- Transition helper interoperability (`TransformLifecycleTransition`, `SceneLifecycleTransition`) using production adapters.
- Null argument guard behavior for production adapter methods.

## 6) Behavior compatibility
- Engine behavior unchanged.
- Adapters are opt-in; nothing is wired into runtime.
- No migration of existing runtime paths was introduced.

## 7) Validation results
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: no.
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: no.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: no.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16h-engine-focused.log`
  - exit: 0; pass; first meaningful warning/error: existing nullability warnings in legacy engine code; output truncated: yes.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - exit: 0; pass; first meaningful warning/error: existing analyzer/obsolete warnings outside scope; output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: no.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - exit: 0; pass; first meaningful warning/error: existing obsolete warnings in test code; output truncated: yes.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - exit: 0; pass; first meaningful warning/error: existing warnings in dependencies; output truncated: yes.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit: 0; pass; first meaningful warning/error: existing warnings in dependencies; output truncated: yes.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: yes.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: yes.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: yes.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0; pass; first meaningful warning/error: one skipped test; output truncated: yes.
- `./striv/build/striv-build-core.sh`
  - exit: 0; pass; first meaningful warning/error: none; output truncated: yes.

## 8) Recommended next task (M16i)
Recommend: processor lifecycle bridge proof first (smallest next production actuator boundary), then root-scene lifecycle bridge proof, then a bounded opt-in integration test path using production adapters.
