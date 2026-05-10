# 2150 — M22c Stride.Engine lifecycle-locality physical move

## 1) Files changed
M22c moved high-confidence lifecycle groups physically (namespaces preserved).

- CloneLifecycle: moved 4 files from `Engine/Design/*` to `Engine/CloneLifecycle/*`.
- SceneLifecycle: moved `Scene.cs`, `SceneInstance.cs`, `SceneSystem.cs`, `Prefab.cs` to `Engine/SceneLifecycle/*`.
- GameLifecycle: moved `Game.cs`, `GameSystem.cs`, `InputSystem.cs`, `Module.cs`, `GameSettings.cs`, `IGameSettingsService.cs` to `Engine/GameLifecycle/*`.
- EntityLifecycle: moved high-confidence entity files and processor files into `Engine/EntityLifecycle/*` and `Engine/EntityLifecycle/Processors/*`.
- RenderingLifecycle: moved high-confidence runtime rendering files from `Engine/*` and `Rendering/*` into `Engine/RenderingLifecycle/*` (including subfolders `Background`, `Compositing`, `LightProbes`, `Lights`, `Skyboxes`, `Sprites`).
- AnimationLifecycle: moved all `Animations/*.cs` into `Engine/AnimationLifecycle/*`.
- DiagnosticsProfilingLifecycle: moved all `Profiling/*.cs` into `Engine/DiagnosticsProfilingLifecycle/*`.
- UpdaterReflection: moved all `Updater/*.cs` into `Engine/UpdaterReflection/*`.
- Quarantine: moved high-confidence excluded audio/shader/VR files into `Engine/Quarantine/*`.

Also changed:
- `striv/projects/Stride.Engine/Stride.Engine.csproj` (only `Compile Remove` path updates for moved excluded files).
- `docs/stri-v/audits/1000+/2150-m22c-stride-engine-lifecycle-locality-physical-move.md` (this report).

## 2) Task scope
This pass was physical locality sorting only:
- no namespace changes;
- no behavior changes;
- no nullability cleanup;
- no public API changes;
- no Dominatus migration.

## 3) Batch summary
- Batch A (CloneLifecycle): moved 4/4; build passed.
- Batch B (SceneLifecycle): moved 4/4; build passed.
- Batch C (GameLifecycle): moved listed files; build passed.
- Batch D (EntityLifecycle): moved listed high-confidence files and processors; build passed.
- Batch E (RenderingLifecycle): moved high-confidence included rendering files; left `EditorTopLevelCompositor.cs` in place; build passed.
- Batch F (AnimationLifecycle): moved all `Animations/*.cs`; build passed.
- Batch G (DiagnosticsProfilingLifecycle): moved 5/5 profiling files; build passed.
- Batch H (UpdaterReflection): moved all `Updater/*.cs`; build passed.
- Batch I (Quarantine): moved excluded audio/shader/VR files and updated remove paths; build passed.

Skipped in-place due caution/risk for later pass:
- Shared and ambiguous files (`Engine/Design/*` shared metadata utilities, `Internals/LambdaReadOnlyCollection.cs`, `Properties/AssemblyInfo.cs`, `FlexibleProcessing/*`).
- Network subtree (classified quarantine candidate in map but still compile-included).
- `Rendering/Compositing/EditorTopLevelCompositor.cs` (excluded but not part of this confident move set).

## 4) Project/include behavior
- Wildcard compile include continued to discover moved included files without explicit include edits.
- `Compile Remove` entries were updated to new Quarantine paths for moved excluded files:
  - `Shaders.Compiler/**/*.cs` -> `Engine/Quarantine/Shaders.Compiler/**/*.cs`
  - `Audio/*.cs` -> `Engine/Quarantine/Audio/*.cs`
  - `Engine/AudioEmitterComponent.cs` -> `Engine/Quarantine/AudioEmitterComponent.cs`
  - `Engine/AudioListenerComponent.cs` -> `Engine/Quarantine/AudioListenerComponent.cs`
  - VR remove paths under `Rendering/Compositing/*` -> `Engine/Quarantine/RenderingVR/*`

## 5) Namespace/API preservation
- Namespaces preserved in moved files.
- No public API edits.
- No type renames/splits/partial-class changes.

## 6) Warning/path result
- Focused warning capture file generated: `/tmp/striv-m22c-engine-warning-lines.log`.
- Warning count file lines: 724.
- Warning identities remained warning-only; paths now reflect new lifecycle folders (e.g. `Engine/AnimationLifecycle/*`, `Engine/CloneLifecycle/*`, `Engine/EntityLifecycle/*`, `Engine/RenderingLifecycle/*`, `Engine/UpdaterReflection/*`, `Engine/DiagnosticsProfilingLifecycle/*`).

## 7) Skipped/ambiguous/shared files
Intentionally left unmoved in M22c:
- Shared candidates in `Engine/Design/*` (except confidently mapped clone/game files).
- `Engine/FlexibleProcessing/*`.
- `Internals/LambdaReadOnlyCollection.cs`.
- `Properties/AssemblyInfo.cs`.
- Network files under `Engine/Network/*` (deferred quarantine decision).
Reason: preserve move-only safety and avoid forced ownership decisions for shared/ambiguous surfaces.

## 8) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m22c-engine-build.log` | 0 | `AnimationChannel.cs(87,24): warning CS8767` | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal 2>&1 | tee /tmp/striv-m22c-engine-tests.log` | 0 | none | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | warning-only existing nullability backlog | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | warning-only existing backlog | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | warning-only existing backlog | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | warning-only existing backlog | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | No |

## 9) Next task recommendation
Recommend **M22d sort Shared/NeedsAudit files** next, now that major lifecycle-local folders are physically established and warning paths are localized.
