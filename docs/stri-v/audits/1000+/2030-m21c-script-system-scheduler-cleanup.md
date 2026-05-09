# 2030-m21c-script-system-scheduler-cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/Processors/ScriptSystem.cs`
- `striv/tests/Stride.Engine.Tests/ScriptSystemLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2030-m21c-script-system-scheduler-cleanup.md`

## 2) Task scope
This pass investigated the deferred `ScriptSystem.Scheduler = null` lifecycle cleanup site as a narrow Category 2 / Category 5-adjacent concern. No scheduler rewrite, no Dominatus migration, and no broad nullability sweep were performed.

## 3) Before warnings
Command:
`dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`

- Focused warning lines before: **950** (`/tmp/striv-m21c-engine-warning-lines-before.log`).
- ScriptSystem warning lines before included:
  - `ScriptSystem.cs(57,25): CS8625` (`Scheduler = null` in `Destroy`)
  - `ScriptSystem.cs(80,60): CS8625`
  - `ScriptSystem.cs(108,50): CS8601`
  - `ScriptSystem.cs(95,76): CS0675`
  - `ScriptSystem.cs(105,57): CS0675`
  - `ScriptSystem.cs(232,56): CS8625`
  - `ScriptSystem.cs(265,39): CS8625`

Relevant global warning code distribution before (focused): `CS8618`, `CS8625`, `CS8604`, `CS8602`, `CS8600`, `CS8603`, ...

## 4) ScriptSystem lifecycle findings
Inspection summary from `ScriptSystem.cs`:

- **Type of `Scheduler`**: `Stride.Core.MicroThreading.Scheduler`.
- **Visibility**: public getter surface (`public Scheduler Scheduler`).
- **Initialization**: constructor-initialized (`new Scheduler()` + event hook).
- **Read sites after creation**: heavily used by `Update`, `NextFrame`, `AddTask`, `WhenAll`, `Remove`, unschedule paths, and exception handling.
- **`Scheduler = null` usage**: teardown-only in `Destroy`; not used as active runtime state while system runs.
- **Disposed flag**: inherited `IsDisposed` exists on `DisposeBase`; `ScriptSystem` had no custom disposed guard for scheduler access.
- **Post-destroy expectations**: prior code exposed non-nullable property that could become null after `Destroy` (contract mismatch). No explicit policy for post-destroy property access.
- **Testability**: direct construction with `new ServiceRegistry()` is sufficient; disposal behavior can be tested without full game runtime.

Conclusion: this specific `Scheduler = null` site is a safe Category 2-style cleanup candidate, with explicit guarded accessor semantics reducing lifecycle ambiguity.

## 5) Tests
Added:
- `ScriptSystem_Scheduler_IsAvailableBeforeDestroy`
- `ScriptSystem_Destroy_IsIdempotent`
- `ScriptSystem_Scheduler_AfterDestroy_ThrowsObjectDisposedException`

These are contained in `striv/tests/Stride.Engine.Tests/ScriptSystemLifecycleTests.cs` and run in the existing `Stride.Engine.Tests` project.

## 6) Fix
Old pattern:
- Auto-property `Scheduler` was non-nullable.
- `Destroy` performed `Scheduler = null`.

New pattern:
- Nullable backing field `scheduler`.
- Guarded accessor: `Scheduler => scheduler ?? throw new ObjectDisposedException(nameof(ScriptSystem));`
- `Destroy` now performs guarded teardown (`if (scheduler is not null)`), unsubscribes, disposes, and nulls backing field.

Behavior preserved:
- Scheduler is created and used exactly as before during normal lifetime.
- Teardown still releases scheduler reference.
- Post-destroy access becomes deterministic (`ObjectDisposedException`) instead of non-nullability contract drift.

## 7) After warnings
Command:
`dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`

- Focused warning lines after: **948** (`/tmp/striv-m21c-engine-warning-lines-after.log`).
- Delta: **-2** focused warning lines.
- ScriptSystem movement:
  - Removed `ScriptSystem.cs(57,25): CS8625` (destroy-site null assignment warning no longer present at that location).
  - Remaining ScriptSystem warnings are unrelated to this lifecycle guard cleanup (`CS8625` at startup node nulling/other fields, `CS8601`, `CS0675`).

## 8) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - Exit: `0`
  - First meaningful warning/error: existing `Stride.Engine` nullability warnings during build
  - Result: pass
  - Output truncated: yes

- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
  - Exit: `0`
  - First meaningful warning/error: existing `CS8765` in `CompressedTimeSpan`
  - Result: pass
  - Output truncated: yes

- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - Exit: `0`
  - First meaningful warning/error: existing `CS1030` perf warning in `Stride.Core.AssemblyProcessor`
  - Result: pass
  - Output truncated: yes

- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - Exit: `0`
  - First meaningful warning/error: none (all listed as pass, 0 warnings)
  - Result: pass
  - Output truncated: no

- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- `./striv/build/striv-build-core.sh`
  - Executed sequentially in one shell chain; final exit: `0`
  - First meaningful warning/error: existing upstream warnings in Stride/Stride.Graphics/Stride.Shaders during build phases
  - Result: pass
  - Output truncated: yes

## 9) Recommended next task
Continue Category 2 with similarly isolated lifecycle contract fixes where teardown nulling conflicts with non-nullable public surfaces, before moving into broader `SceneInstance/SceneSystem` lifecycle work.
