# 2070 — M21g SceneSystem PropertyKey boundary cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/SceneSystem.cs`
- `docs/stri-v/audits/1000+/2070-m21g-scenesystem-propertykey-boundary-cleanup.md`

## 2) Task scope
Targeted SceneSystem lifecycle/nullability cleanup only. No SceneSystem rewrite, no render/content pipeline redesign, no warning suppression.

## 3) Before warnings
- Focused warning lines before: **856** (`/tmp/striv-m21g-engine-warning-lines-before.log`).
- SceneSystem warning lines before: CS8602 at lines 174/275 and CS8620 at lines 236/252 (duplicated by multi-target build output).
- Relevant warning codes in focused set included CS8620/CS8602 among larger baseline buckets.

## 4) PropertyKey/tag findings
- `GraphicsCompositor.Current` type: `PropertyKey<GraphicsCompositor>`.
- `SceneInstance.Current` type: `PropertyKey<SceneInstance>`.
- `PushTagAndRestore<T>` signature expects `PropertyKey<T?> key` with non-null `T value`.
- SceneSystem can intentionally have null current compositor/scene values at lifecycle boundaries.
- The warnings are generic nullability-boundary mismatches between non-null key declarations and nullable-friendly push API, not an immediate runtime null dereference at the callsite after non-null guards.

## 5) Classification table
| Site | Warning | Key/value type | Null possible? | Action |
| ---- | ------- | -------------- | -------------: | ------ |
| `GraphicsCompositor.Current` push | CS8620 | `PropertyKey<GraphicsCompositor>` + guarded `GraphicsCompositor` | Yes (lifecycle) | Guarded push implemented; mismatch remains due key type contract. |
| `SceneInstance.Current` push | CS8620 | `PropertyKey<SceneInstance>` + guarded `SceneInstance` | Yes (lifecycle) | Guarded push implemented; mismatch remains due key type contract. |

## 6) Fixes or deferral
- Updated SceneSystem draw flow to push tags only when non-null values exist; otherwise execute equivalent draw path without tag push.
- Added intro splash clear render-target guard.
- **Deferral remains for CS8620:** root issue is API contract mismatch (`PropertyKey<T>` declarations vs nullable-friendly push signature). Resolving cleanly requires broader render-tag key contract audit beyond this localized pass.

## 7) Tests
- No new tests added in M21g. Existing `Stride.Engine.Tests` suite executed and passed.

## 8) After warnings
- Focused warning lines after: **856** (`/tmp/striv-m21g-engine-warning-lines-after.log`).
- SceneSystem warning delta: **0**.
- Total focused delta: **0**.

## 9) Next bucket analysis (M21h recommendation)
Top buckets from `...warning-buckets-after.log`:
- `Engine/ScriptComponent.cs CS8618` (28 lines): high-yield constructor/lifecycle initialization bucket; mostly deterministic field initialization; low-medium risk; good testability via engine tests.
- `Rendering/Compositing/ForwardRenderer.cs CS8618` (24 lines): large yield but higher rendering-lifecycle risk.
- `Engine/Design/CloneSerializer.cs CS8602` (20 lines): moderate yield, medium logic risk.

**Recommended M21h:** `Engine/ScriptComponent.cs` CS8618 bucket (best yield/risk ratio for targeted nullability progress).

## 10) Validation results
(See command history in this run; all executed with exit code 0.)
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
