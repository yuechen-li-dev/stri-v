# M21i — Stride.Engine rendering subsystem lifetime/nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Rendering/Compositing/ForwardRenderer.cs`
- `striv/tests/Stride.Engine.Tests/ForwardRendererLifecycleTests.cs`

## 2) Task scope
Focused on `Stride.Engine` rendering subsystem lifetime/nullability, beginning with `ForwardRenderer` and related in-project rendering patterns. No render-pipeline rewrite, no Dominatus migration, no runtime behavior rewiring.

## 3) Before warnings
- Focused warning count before: **806** (`/tmp/striv-m21i-engine-warning-lines-before.log`).
- Rendering-relevant top bucket before: `Rendering/Compositing/ForwardRenderer.cs CS8618` with **24** lines.
- Top relevant warning codes before: `CS8618`, `CS8625`, `CS8602`, `CS8604`, `CS8603`.

## 4) Rendering subsystem classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| Rendering/Compositing/ForwardRenderer.cs stage/effect properties | CS8618 | Optional links not initialized at construction | Default construction should be inert, optional links unset until configured | Optional renderer/compositor connection | Converted optional properties to nullable (`RenderStage?`, `IPostProcessingEffects?`, etc.) |
| Rendering/Compositing/ForwardRenderer.cs runtime textures | CS8618 | Runtime/load initialized render resources | Valid only during draw path after render-target prep | Render resource lifecycle | Nullable lifecycle fields (`Texture?`) retained with guarded flow; no fake defaults |
| Rendering/ModelRenderProcessor.cs | CS8618/CS8604 | Runtime-initialized service/material lifecycle | Requires engine services/system add lifecycle | Needs render lifecycle audit | Deferred for M21j+ (higher-risk service lifecycle) |
| Rendering/Compositing/ForwardRenderer.LightProbes.cs | CS8618/CS8604 | Pipeline/lightprobe GPU-state init | Requires graphics device/runtime pipeline | Needs render lifecycle audit | Deferred; requires device-context-aware refactor |

## 5) Tests
Added constructor/lifecycle tests in `ForwardRendererLifecycleTests`:
- `ForwardRenderer_DefaultConstruction_HasSafeDefaultConfiguration`
- `ForwardRenderer_DefaultConstruction_LeavesOptionalRenderLinksUnset`

These pin inert default construction and explicit optional-link semantics without requiring graphics device setup.

## 6) Fixes applied
### ForwardRenderer.cs
- **Old pattern:** non-nullable fields/properties for optional/lifecycle resources caused CS8618 and implied always-on links.
- **New pattern:** optional render stages/effects and load/draw-time texture caches are explicitly nullable.
- **Why correct:** semantically matches renderer lifecycle (configuration + draw-time allocation), avoids fake initialization and preserves behavior.

## 7) Deferred rendering lifecycle issues
- `ModelRenderProcessor` service/material lifecycle (`OnSystemAdd` dependency and runtime mesh/material passes).
- `ForwardRenderer` remaining CS8604 path warnings in draw pipeline where device-state guarantees are implicit.
- `ForwardRenderer.LightProbes` GPU pipeline object/state lifecycle requires render-device-bound assertions.

## 8) After warnings
- Focused warning count after: **788** (`/tmp/striv-m21i-engine-warning-lines-after.log`).
- Rendering subsystem delta: `ForwardRenderer.cs CS8618` bucket removed; file now dominated by CS8604/CS8625 lifecycle-flow warnings.
- Total focused delta: **-18**.

## 9) Next bucket recommendation (M21j)
1. `Engine/Design/CloneSerializer.cs CS8602` (20) — high count, non-rendering, highly testable, low runtime risk.
2. `Engine/Game.cs CS8602` (16) — high count, medium risk, broad impact.
3. Rendering follow-up: `Rendering/ModelRenderProcessor.cs CS8604` (14) once lifecycle tests for material/service state are added.

Recommended M21j primary target: **CloneSerializer CS8602**.

## 10) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | --- | --- | --- | --- |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` (before) | 0 | Existing focused nullability warnings (baseline) | Pass | Yes |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` (after) | 0 | Remaining lifecycle-flow warnings (CS8604/CS8625) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | Existing solution warnings outside scope | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | Existing CS0618 test warnings | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | Yes |
