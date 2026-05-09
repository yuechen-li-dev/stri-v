# M18d — Sample-style composed Dominatus runtime node

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus/Nodes/EngineLifecycleDominatusNodes.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EngineLifecycleRuntimeTests.cs`

`Stride.Engine` production files changed: **none**.

## 2) Task scope
Implemented a small composed Dominatus node that sequentially drives scene attach, transform parenting, and processor lifecycle via `Ai.Act(...)` commands, then validated it through real Dominatus runtime + production adapters only.

## 3) Sample style alignment
From Ariadne/Fishtank samples, this milestone keeps:
- plain iterator node method returning `IEnumerator<AiStep>`;
- sequential `yield return Ai.Act(...)` for side effects;
- no direct state mutation in node;
- runtime-driven execution through handler registration + `world.Tick(...)`.

## 4) Node design
Node sequence encodes:
1. attach parent to scene;
2. attach child to scene;
3. attach child transform parent;
4. add processor to entity system;
5. add child entity to processor.

## 5) Runtime test
The runtime test:
- builds `ActuatorHost`, `AiWorld`, `AiAgent`, `HfsmInstance`;
- registers scene/transform/processor actuation handlers;
- uses production adapters (`StrideSceneLifecycleActuator`, `StrideTransformLifecycleActuator`, `StrideProcessorLifecycleActuator`);
- ticks runtime once;
- asserts scene membership on parent, child scene pointer, transform parent/child linkage, processor manager linkage, and processor add callback/entity.

## 6) Ordering doctrine
Scene attach before transform parenting is explicit in the composed node order and exercised in runtime test.

## 7) Behavior compatibility
- No engine behavior changes.
- No direct Dominatus dependency added to `Stride.Engine`.
- Adapters remain opt-in at Dominatus integration layer.

## 8) Runtime harness observation
Harness extraction deferred. Current duplication is still small; a shared helper could be useful in M18e if more composed runtime tests are added.

## 9) Validation results
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: warnings from existing Stride projects (no new M18d error)
  - pass/fail: pass
  - output truncated: yes
- `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m18d-engine-focused.log`
  - exit: 0
  - first meaningful warning/error: existing nullability warnings in `Stride.Engine`
  - pass/fail: pass
  - output truncated: yes
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: existing warnings in legacy/shared projects
  - pass/fail: pass
  - output truncated: yes
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - exit: 0
  - first meaningful warning/error: none in focused summary
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: existing obsolete-test warnings
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes
- `./striv/build/striv-build-core.sh`
  - exit: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: yes

## 10) Recommendation
Proceed with **M18e runtime harness extraction** as a small test-only refactor candidate, since composed runtime tests are now repeating the same world/agent/graph bootstrapping pattern.
