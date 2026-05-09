# 2090 — M21i Stride.Engine rendering subsystem cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Rendering/Compositing/ForwardRenderer.cs`
- `striv/tests/Stride.Engine.Tests/ForwardRendererLifecycleTests.cs`

## 2) Task scope
Focused nullability/lifetime cleanup in Stride.Engine rendering subsystem, centered on `ForwardRenderer` and nearby rendering warning clusters. No render pipeline rewrite, no Dominatus migration, and no runtime rewiring.

## 3) Before warnings
- Focused warning count before: **788** (`/tmp/striv-m21i-engine-warning-lines-before.log`).
- Rendering-relevant top buckets before included:
  - `Rendering/Compositing/ForwardRenderer.cs CS8604` (22)
  - `Rendering/ModelRenderProcessor.cs CS8604` (14)
  - `Rendering/Compositing/ForwardRenderer.LightProbes.cs CS8604` (10)
- Top focused warning codes before:
  - CS8618 (250), CS8625 (108), CS8602 (88), CS8604 (86)

## 4) Rendering subsystem classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| `Rendering/Compositing/ForwardRenderer.cs` | CS8604/CS8602/CS8625 | Render-target/depth resources are lifecycle-initialized and used later | Fail deterministically if used before initialization; no accidental NRE | Render resource lifecycle | Added explicit guards and deterministic `InvalidOperationException` checks at use boundaries |
| `Rendering/Compositing/ForwardRenderer.cs` | CS8604 | Optional output target/depth passed to post/copy paths | Only draw/copy when required targets exist | Runtime/load-content initialized field | Added pre-use guard checks before `PostEffects.Draw` and resolve/copy paths |
| `Rendering/ModelRenderProcessor.cs` | CS8618/CS8604 | Processor fields assigned in system lifecycle | Requires engine service wiring | Needs render lifecycle audit | Deferred for dedicated follow-up bucket |
| `Rendering/Compositing/ForwardRenderer.LightProbes.cs` | CS8618/CS8600/CS8604 | Light-probe resources initialized during rendering setup | Requires graphics-device-backed runtime setup | Needs render lifecycle audit | Deferred |

## 5) Tests
- Existing constructor-safety tests were extended in `ForwardRendererLifecycleTests`.
- Added `ForwardRenderer_DefaultConstruction_DoesNotRequireGraphicsDeviceForConfigurationAccess` to pin safe pre-init configuration access.
- No graphics-device draw-path tests were added in this pass, because those paths require full rendering/runtime setup rather than lightweight unit construction.

## 6) Fixes applied
### `ForwardRenderer.cs`
- Old pattern: nullable render resources used in post/resolve/copy flows with implicit assumptions.
- New pattern: explicit guard locals/throws before MSAA resolve, light shafts draw, post effects draw, copy, and set-render-target transitions.
- Why correct: these resources are lifecycle-initialized; deterministic guard failures reflect intended contract and avoid accidental null dereferences.

### `ForwardRendererLifecycleTests.cs`
- Old pattern: default-construction assertions covered only initial object graph.
- New pattern: added configuration-access test to pin inert/safe default behavior pre-graphics-device setup.
- Why correct: validates intended public behavior for construction-time configuration.

## 7) Deferred rendering lifecycle issues
- `ModelRenderProcessor` lifecycle-bound fields (`fallbackMaterial`, `VisibilityGroup`) require processor/system-phase treatment.
- `ForwardRenderer.LightProbes` warnings depend on GPU resource setup and probe pipeline state.
- Some compositor/render-stage contracts remain nullable due to configuration semantics and should be handled in separate scoped buckets.

## 8) After warnings
- Focused warning count after: **774** (`/tmp/striv-m21i-engine-warning-lines-after.log`).
- Rendering delta observed:
  - `ForwardRenderer.cs CS8604`: **22 → 8**.
- Total focused delta: **-14**.

## 9) Next bucket recommendation (M21j)
1. `Engine/Design/CloneSerializer.cs CS8602` (20) — high count, pure non-rendering logic, good testability.
2. `Engine/Game.cs CS8602` (16) — similarly high count, central lifecycle behavior.
3. `Rendering/ModelRenderProcessor.cs CS8604` (14) — keep as next rendering follow-up once processor lifecycle tests are scaffolded.

Recommended M21j: **`Engine/Design/CloneSerializer.cs CS8602`** due to high reduction potential and low rendering-runtime risk.

## 10) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` — exit 0 — pass.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` — exit 0 — pass (warnings only).
- Additional focused warning extraction/aggregation shell commands completed successfully (exit 0).
