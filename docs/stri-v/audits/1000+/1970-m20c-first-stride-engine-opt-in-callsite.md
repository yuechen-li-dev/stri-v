# 1970 — M20c first Stride.Engine opt-in callsite

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/Lifecycle/IEntityLifecycleOrchestrator.cs`
- `striv/projects/Stride.Engine/Engine/EntityManager.cs`
- `striv/projects/StriV.Engine.Dominatus.Runtime/IEntityLifecycleOrchestrator.cs`
- `striv/tests/Stride.Engine.Tests/EntityLifecycleOrchestratorCallsiteTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleOrchestratorCallsiteIntegrationTests.cs`
- `docs/stri-v/audits/1000+/1970-m20c-first-stride-engine-opt-in-callsite.md`

## 2) Task scope
This change adds the first production-side, engine-owned opt-in callsite for entity lifecycle orchestration. It does not alter default engine behavior and does not wire any orchestrator by default. `Stride.Engine` remains Dominatus-agnostic.

## 3) Engine seam design
- Added engine-owned neutral seam under `Stride.Engine/Engine/Lifecycle/`.
- Added opt-in `EntityManager.RunEntityLifecycleFullCycleAsync(...)` method that delegates to caller-supplied orchestrator and passes `this` manager.
- Method is explicit opt-in and does not affect existing runtime paths.
- Interface exposes only Stride + BCL types; no Dominatus leakage.

## 4) Runtime integration
Chose low-risk **Option A**: keep runtime `IEntityLifecycleOrchestrator` and make it inherit `Stride.Engine.Lifecycle.IEntityLifecycleOrchestrator`. This preserves existing runtime/test callsites while binding to the engine-owned seam. `DominatusEntityLifecycleOrchestrator` continues to delegate to `StriVEngineLifecycleRunner` unchanged in behavior.

## 5) Tests
- Engine-only delegation proof:
  - `EntityManager_RunEntityLifecycleFullCycle_DelegatesToOrchestratorWithThisManager`
  - `EntityManager_RunEntityLifecycleFullCycle_RejectsNullOrchestrator`
- Dominatus-backed callsite integration proof:
  - `EntityManager_RunEntityLifecycleFullCycle_WithDominatusOrchestrator_RunsFullCycle`
  - Asserts same full-cycle terminal state as runner full-cycle test (scene detach, transform detach, processor removal, add/remove counts, child identity tracking).

## 6) Dependency boundary verification
Command:
`rg -n "StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`

Result: no matches.

## 7) Behavior compatibility
- Default engine behavior unchanged.
- New callsite is explicit and opt-in only.
- Dominatus adapters/orchestrator remain external to `Stride.Engine`.

## 8) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m20c-engine-focused.log`
  - Exit: 0
  - First meaningful warning: `CS8767` in `Animations/AnimationChannel.cs`
  - Status: pass
  - Output truncated: yes
- `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: no
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: no
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: no
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - Exit: 0
  - First meaningful warning: `CS1030` in `sources/core/Stride.Core/Storage/ObjectIdBuilder.cs`
  - Status: pass
  - Output truncated: yes
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - Exit: 0
  - First warning/error: none in focused summary
  - Status: pass
  - Output truncated: no
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: no
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: no
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - Exit: 0
  - First meaningful warning: `CS0618` in `TypeDescriptorFactoryCollectionFallbackTests.cs`
  - Status: pass
  - Output truncated: yes
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes
- `./striv/build/striv-build-core.sh`
  - Exit: 0
  - First warning/error: none
  - Status: pass
  - Output truncated: yes

## 9) Recommended next task
**M20d parity test comparing legacy direct lifecycle vs orchestrated path**: add a focused comparison test around a concrete production flow to prove behavioral equivalence while keeping callsite opt-in.
