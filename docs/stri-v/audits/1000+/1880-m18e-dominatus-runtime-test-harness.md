# M18e — Dominatus runtime test harness extraction

## 1) Files changed
- Added test-only harness:
  - `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/DominatusRuntimeTestHarness.cs`
- Refactored runtime tests:
  - `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/TransformLifecycleRuntimeTests.cs`
  - `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/SceneLifecycleRuntimeTests.cs`
  - `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/ProcessorLifecycleRuntimeTests.cs`
  - `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EngineLifecycleRuntimeTests.cs`

Production files changed: **no**.

## 2) Task scope
This change extracts repeated Dominatus runtime bootstrap in tests only (actuator host registration, one-state graph/agent/world creation, and tick execution). It does not introduce a production runtime abstraction and does not alter engine runtime behavior.

## 3) Sample style alignment
The refactor preserves Ariadne/FishTank style:
- nodes remain plain `IEnumerator<AiStep>` methods;
- side effects still execute via actuation handlers/adapters;
- tests still host runtime explicitly (`ActuatorHost` + `AiWorld` + `AiAgent` + tick), now through a thin helper.

## 4) Harness design
Harness owns only:
- handler registration on `ActuatorHost`;
- one-state `HfsmGraph` + `AiAgent` construction;
- `AiWorld` creation/add/initialize;
- a small deterministic tick helper.

Harness deliberately does not own:
- scene/transform/processor policy;
- bridge/adapter behavior;
- production runtime configuration.

It is test-only (`StriV.Engine.Dominatus.Tests` namespace and project) to reduce duplication while keeping runtime wiring visible.

## 5) Refactored tests and assertions
Updated:
- transform runtime attach test;
- scene runtime attach and scene+transform composed test;
- processor add-system, add-entity, and composed processor tests;
- composed engine lifecycle runtime test.

Assertions were preserved (transform parent/children, scene membership, processor manager binding, add callback counts/entities, and composed end-state checks).

## 6) Behavior compatibility
- No engine behavior changed.
- No direct Dominatus dependency was added to `Stride.Engine`.
- Adapter path remains opt-in through existing Dominatus adapter handlers.

## 7) Runtime harness observations
- Repetition in runtime tests is materially reduced (graph/host/world/tick boilerplate removed from four files).
- Further extraction is not currently necessary; helper is intentionally small and transparent. Stop before framework overreach.

## 8) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
   - exit: 0
   - first meaningful warning/error: existing legacy warnings in transitive Stride projects
   - pass/fail: pass
   - output truncated: yes

2. `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

3. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

4. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m18e-engine-focused.log`
   - exit: 0
   - first meaningful warning/error: existing Stride nullability warning (`CompressedTimeSpan.cs`)
   - pass/fail: pass
   - output truncated: yes

5. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - exit: 0
   - first meaningful warning/error: existing warnings in legacy/transitive projects
   - pass/fail: pass
   - output truncated: yes

6. `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
   - exit: 0
   - first meaningful warning/error: none (all focused checks pass)
   - pass/fail: pass
   - output truncated: no

7. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: yes

8. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - exit: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: yes

9. `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
   - exit: 0
   - first meaningful warning/error: existing obsolete/test fixture warnings
   - pass/fail: pass
   - output truncated: yes

10. `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

11. `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

12. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

13. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

14. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

15. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

16. `./striv/build/striv-build-core.sh`
    - exit: 0
    - first meaningful warning/error: none
    - pass/fail: pass
    - output truncated: yes

## 9) Recommendation
**M18f composed runtime cleanup/remove sequence**.

Reason: now that runtime bootstrap duplication is reduced and tests still validate composed behavior through real runtime execution, the next low-risk step is to clean up remaining composed runtime test sequencing/helpers (without broadening into production abstractions yet).
