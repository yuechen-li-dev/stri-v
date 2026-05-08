# 1680 — M16b StriV.Engine.Dominatus bridge validation

## 1) Files changed
- Added `striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj`.
- Added `striv/projects/StriV.Engine.Dominatus/README.md`.
- Added bridge source under:
  - `Blackboard/EngineBlackboardKeys.cs`
  - `Events/*.cs`
  - `Actuators/*.cs`
  - `Nodes/*.cs`
- Added `striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj`.
- Added `striv/tests/StriV.Engine.Dominatus.Tests/BridgeSurfaceTests.cs`.
- Updated `striv/StriV.Core.slnx` with both new projects.

## 2) Task scope
This change set is bridge-project creation only. No runtime behavior migration was performed in `Stride.Engine` or `Stride.Games`. No `Dominatus.StrideConn` usage was introduced. No runtime rewiring performed.

## 3) Architecture decision
Dominatus is not integrated as a Stride AI-module plugin surface in this task. Instead, `StriV.Engine.Dominatus` introduces Stri-V-owned bridge contracts to formalize future lifecycle/HFSM refactors through explicit events, blackboard keys, actuator interfaces, and node skeletons.

## 4) Project references
### Included
- `external/Dominatus/src/Dominatus.Core/Dominatus.Core.csproj`
- `external/Dominatus/src/Dominatus.OptFlow/Dominatus.OptFlow.csproj`
- `external/Dominatus/src/Dominatus.UtilityLite/Dominatus.UtilityLite.csproj`
- `projects/Stride.Engine/Stride.Engine.csproj`

### Excluded
- `Dominatus.StrideConn`
- `Dominatus.Server`
- `Dominatus.Actuators.*`
- `Dominatus.Llm.OptFlow`

## 5) Bridge surface
- Blackboard keys: engine/scene/entity/processor lifecycle placeholders and key engine objects.
- Events/messages: engine, scene, entity, processor lifecycle records.
- Actuator interfaces: minimal async contracts for start/stop, attach/detach, add/remove operations.
- Node skeletons: four compile-clean idle node stubs using Dominatus `Ai.Wait(...)`.

## 6) Tests
`BridgeSurfaceTests` proves:
- lifecycle event records instantiate,
- key names are exposed,
- actuator interfaces are implementable,
- node skeleton entry points are present,
- Dominatus blackboard accepts bridge key usage.

## 7) Behavior compatibility
- No `Stride.Engine` runtime behavior changed.
- No `Stride.Games` runtime behavior changed.
- No direct Dominatus dependency added to `Stride.Engine`; dependency is isolated to the new bridge project.
- No runtime migration was attempted.

## 8) Validation results
- `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
  - Exit: 0
  - First meaningful warning/error: existing repo warnings from legacy Stride projects during transitive build.
  - Pass/Fail: Pass
  - Output truncated: Yes
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: No
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - Exit: 0
  - First meaningful warning/error: existing repo warnings
  - Pass/Fail: Pass
  - Output truncated: Yes
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: No
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning/error: existing obsolete-warning coverage in tests
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - Exit: 0
  - First meaningful warning/error: one expected skipped test
  - Pass/Fail: Pass
  - Output truncated: Yes (aggregated command output)
- `./striv/build/striv-build-core.sh`
  - Exit: 0
  - First meaningful warning/error: none
  - Pass/Fail: Pass
  - Output truncated: Yes

## 9) Recommended next task (M16c)
Recommend first strangler proof on **Entity/Transform/Scene attach-detach lifecycle model**.

Why this first:
- small and locally bounded lifecycle transitions,
- naturally maps to explicit events + actuator operations already scaffolded,
- low blast radius compared to script scheduler or remote compiler client,
- high leverage for validating Dominatus-based explicit state transitions without broad runtime rewiring.
