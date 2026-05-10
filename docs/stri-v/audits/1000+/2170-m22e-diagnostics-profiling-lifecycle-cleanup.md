# M22e diagnostics profiling lifecycle cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/DiagnosticsProfilingLifecycle/GameProfilingSystem.cs`
- `striv/projects/Stride.Engine/Engine/DiagnosticsProfilingLifecycle/DebugTextSystem.cs`
- `striv/tests/Stride.Engine.Tests/GameProfilingSystemLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2170-m22e-diagnostics-profiling-lifecycle-cleanup.md`

## 2) Task scope
Contained folder-local nullability cleanup in `Engine/DiagnosticsProfilingLifecycle` only, plus tests and this report. No profiling redesign, no telemetry rewrite, no Dominatus migration.

## 3) Before warnings
- Focused warning lines before: `724`
- DiagnosticsProfilingLifecycle warning lines before: repeated entries including `GameProfilingSystem` and `DebugTextSystem` (CS8618/CS8602/CS8625/CS8629).
- Top relevant bucket before: `Engine/DiagnosticsProfilingLifecycle/GameProfilingSystem.cs CS8602` (14), `... CS8618` (8), `... CS8629` (6), `DebugTextSystem.cs CS8602` (6).

## 4) Diagnostics profiling classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
|---|---|---|---|---|---|
| GameProfilingSystem fields | CS8618 | renderer/task/channel/render target initialized later | headless default construction is inert and valid | runtime/game-system initialized state | nullable-annotated optional fields/properties |
| GameProfilingSystem.UpdateProfilingStrings | CS8602 | `Game` dereferenced | must only read times when attached to game | lifecycle guard | early return when `Game == null` |
| GameProfilingSystem.Draw | CS8602/CS8625 | game/content/runtime draw chain deref | draw requires active game runtime | lifecycle guard | explicit `InvalidOperationException` when detached |
| GameProfilingSystem.FilteringMode/Destroy | CS8602 | channel may be absent | unsubscribe only when subscribed | service lookup / lifecycle guard | guarded unsubscribe |
| DebugTextSystem renderer | CS8618/CS8602 | renderer built lazily | renderer optional until first draw | optional debug text renderer/font | nullable renderer + existing creation guard |
| DebugTextSystem draw loop | behavior bug | index `> 0` skipped message 0 | should draw all queued messages | collection/list default | loop changed to `>= 0` |

## 5) Tests
Added lifecycle tests to pin intended constructor/disable behavior:
- `GameProfilingSystem_DefaultConstruction_HasValidInertState`
- `GameProfilingSystem_DisableProfiling_IsIdempotent`

These assert intended lifecycle behavior (inert default and safe disable), not accidental null quirks.

## 6) Fixes applied
- `GameProfilingSystem`: optional runtime fields marked nullable and guarded at use sites.
- `GameProfilingSystem`: added game lifecycle checks before dereferencing `Game` and draw-time dependencies.
- `DebugTextSystem`: lazy renderer field made nullable and draw loop fixed to include index 0.

## 7) Deferred profiling lifecycle issues
Remaining warnings in this folder are tied to runtime display/render lifecycle flow and legacy contracts (`ProfilingEvent?` / content loading / graphics context nullability) that need wider diagnostics lifecycle typing pass.

## 8) After warnings
- Focused warning lines after: `706`
- DiagnosticsProfilingLifecycle warning delta: reduced constructor-nullability warnings, but CS8602/CS8625/CS8629 remain.
- Total delta: `-18` warning lines in focused build output.

## 9) Next folder-local recommendation
From top buckets after, next target recommendation: `Engine/AnimationLifecycle` (high warning density, strongly local APIs, testable with constructor/lifecycle tests). Secondary: `Engine/UpdaterReflection` (high count but higher risk due reflection/IL behavior).

## 10) Validation results
Commands run with exit code 0:
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- `./striv/build/striv-build-core.sh`
