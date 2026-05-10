# 2140 — M22b ScriptLifecycle physical move

## 1) Files changed

Moved exactly the M22a-script-lifecycle files into `Engine/ScriptLifecycle`:

- `striv/projects/Stride.Engine/Engine/ScriptComponent.cs` -> `striv/projects/Stride.Engine/Engine/ScriptLifecycle/ScriptComponent.cs`
- `striv/projects/Stride.Engine/Engine/AsyncScript.cs` -> `striv/projects/Stride.Engine/Engine/ScriptLifecycle/AsyncScript.cs`
- `striv/projects/Stride.Engine/Engine/SyncScript.cs` -> `striv/projects/Stride.Engine/Engine/ScriptLifecycle/SyncScript.cs`
- `striv/projects/Stride.Engine/Engine/StartupScript.cs` -> `striv/projects/Stride.Engine/Engine/ScriptLifecycle/StartupScript.cs`
- `striv/projects/Stride.Engine/Engine/Processors/ScriptProcessor.cs` -> `striv/projects/Stride.Engine/Engine/ScriptLifecycle/ScriptProcessor.cs`
- `striv/projects/Stride.Engine/Engine/Processors/ScriptSystem.cs` -> `striv/projects/Stride.Engine/Engine/ScriptLifecycle/ScriptSystem.cs`

Report file:

- `docs/stri-v/audits/1000+/2140-m22b-script-lifecycle-physical-move.md`

## 2) Task scope

This change is a **physical locality move only**:

- no namespace changes;
- no behavior changes;
- no nullability cleanup;
- no public API changes;
- no type renames/splits/partials.

## 3) Move rationale

`ScriptLifecycle` was selected as first physical move target because:

- it was recently lifecycle-cleaned in M21h;
- it forms a coherent subsystem (script init/start/update/teardown scheduling);
- dependencies are already known and test-covered (`ScriptComponentLifecycleTests`, `ScriptSystemLifecycleTests`);
- move risk is low-to-medium (locality-only, namespace-stable);
- it establishes precedent for future lifecycle-local folder moves.

## 4) Project/include behavior

- `striv/projects/Stride.Engine/Stride.Engine.csproj` already uses explicit wildcard compile include (`<Compile Include="**/*.cs" .../>`) with SDK-style project system and `EnableDefaultCompileItems=false`.
- No `.csproj` edits were required for this move.
- Tests reference script types by namespace/type usage, not file path.
- No build-system include/remove rules needed path updates for these files.

## 5) Namespace/API preservation

Verified post-move headers and declarations:

- `ScriptComponent`, `AsyncScript`, `SyncScript`, `StartupScript` remain in `namespace Stride.Engine`.
- `ScriptProcessor`, `ScriptSystem` remain in `namespace Stride.Engine.Processors`.
- Class/type names unchanged; public API unchanged.

## 6) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m22b-engine-build.log` | 0 | `CompressedTimeSpan.cs(63,30): warning CS8765 ...` | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal 2>&1 | tee /tmp/striv-m22b-engine-tests.log` | 0 | none | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | `ObjectIdBuilder.cs(...): warning CS1030 ...` | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none (`0` warnings for all focused projects) | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | existing warnings only | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | existing warnings only | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | existing warnings only | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | No |

## 7) Warning/path result

- Focused warning lines captured: `724` (`wc -l /tmp/striv-m22b-engine-warning-lines.log`).
- Script-subsystem warning entries now report under `Engine/ScriptLifecycle/` (e.g. `ScriptSystem.cs`, `ScriptProcessor.cs`).
- This task performed a path-locality move only; warning identities remained existing warning classes (no cleanup attempted).

## 8) Next move recommendation

**Recommendation: `CloneLifecycle` physical move next.**

Reasoning:

- M22b was clean (build/test/validation pass, no include breakage);
- lifecycle-local folder move pattern held with namespace/API preservation;
- per guidance, prefer the small/coherent recently cleaned lifecycle target next.
