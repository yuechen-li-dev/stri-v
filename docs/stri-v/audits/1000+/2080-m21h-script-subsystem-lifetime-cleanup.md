# 2080 — M21h script subsystem lifetime/nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/ScriptComponent.cs`
- `striv/projects/Stride.Engine/Engine/AsyncScript.cs`
- `striv/projects/Stride.Engine/Engine/SyncScript.cs`
- `striv/projects/Stride.Engine/Engine/StartupScript.cs`
- `striv/tests/Stride.Engine.Tests/ScriptComponentLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2080-m21h-script-subsystem-lifetime-cleanup.md`

## 2) Task scope
- Scope was the script subsystem nullability/lifetime bucket (ScriptComponent + ScriptSystem-adjacent script types).
- Work was test-first for behavior-shaping changes.
- No scheduler rewrite.
- No Dominatus migration.

## 3) Before warnings
- Focused warning count before: **856** (`/tmp/striv-m21h-engine-warning-lines-before.log`).
- Script-focused before lines included:
  - `Engine/ScriptComponent.cs CS8618` (28)
  - `Engine/Processors/ScriptSystem.cs CS8625` (6), `CS0675` (4), `CS8601` (2)
  - `Engine/AsyncScript.cs CS8618` (4)
  - `Engine/SyncScript.cs CS8618` (2)
  - `Engine/StartupScript.cs CS8618` (2)

## 4) Script subsystem classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| ScriptComponent ctor/service caches | CS8618/CS8601/CS8603/CS8604 | runtime-assigned services and lazy caches | default component is inert/safe; pre-injection access should not NRE | Runtime-assigned lifecycle field | nullable cache fields + nullable service-backed properties + null-safe lazy access |
| ScriptComponent profile/logger names | CS8604 | possible null type fullname | profiling/logging should always get non-null name | Constructor-required state | fallback to `Type.Name` |
| AsyncScript.MicroThread/CancellationTokenSource | CS8618 | runtime-assigned after scheduling | pre-start script exists in inert state | Runtime-assigned lifecycle field | nullable fields, deterministic default token |
| SyncScript.ScriptSystem | CS8618 | attached by system after registration | detached script allowed | Runtime-assigned lifecycle field | mark nullable |
| StartupScript.StartSchedulerNode | CS8618/CS8625 | startup node created/cleared by scheduler lifecycle | node may be absent before start and after removal | Startup/scheduler node state | mark nullable; keep lifecycle semantics |
| ScriptSystem startup-node nulling | CS8604/CS8602 remaining | internal scheduler state transitions | do not pin private null-marker internals as contract | Needs Dominatus migration later | deferred; keep behavior unchanged |

## 5) Tests
Added:
- `ScriptComponent_DefaultConstruction_HasValidEmptyState`
- `ScriptComponent_DefaultConstruction_DoesNotRequireRuntimeInjectionForBasicAccess`

These pin inert default script behavior and safe access semantics, and intentionally avoid asserting internal scheduler node-null implementation details.

## 6) Fixes applied
- Converted runtime-injected/lazy script-service fields to nullable and made public accessors null-safe in `ScriptComponent`.
- Updated service-backed script properties to nullable where pre-registration absence is valid.
- Added robust fallback naming for profiling/logger generation (`FullName ?? Name`).
- Marked startup/sync/async runtime lifecycle fields nullable in `StartupScript`, `SyncScript`, `AsyncScript`.

## 7) Deferred script lifecycle issues
- Remaining `ScriptSystem` warnings include startup/scheduler-node state and batch scheduler flow warnings.
- Deferred because null in these sites encodes lifecycle state machine semantics and should be addressed in future explicit lifecycle modeling (Dominatus direction), not via local nullable spray.

## 8) After warnings
- Focused warning count after: **806** (`/tmp/striv-m21h-engine-warning-lines-after.log`).
- Script subsystem delta: `ScriptComponent` CS8618 bucket removed; async/sync/startup CS8618 buckets removed.
- Total delta: **-50** warning lines.

## 9) Next bucket recommendation (M21i)
Top remaining buckets suggest:
1. `Rendering/Compositing/ForwardRenderer.cs CS8618` (24)
2. `Engine/Design/CloneSerializer.cs CS8602` (20)
3. `Engine/Game.cs CS8602` (16)

Recommended M21i: **ForwardRenderer CS8618** (highest concentration, likely constructor/lifetime mechanical fixes, testable without deep scheduler semantics).

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` → exit 0, pass, output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass, output truncated: yes.
- `./striv/build/striv-build-core.sh` → exit 0, pass, output truncated: yes.
