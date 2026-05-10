# M22o — RenderingLifecycle lights + light-probes nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Lights/LightProcessor.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/LightProbes/LightProbeProcessor.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/ForwardRenderer.LightProbes.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/LightProbeComponent.cs`
- `striv/tests/Stride.Engine.Tests/RenderingLightLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2270-m22o-rendering-lights-probes-cleanup.md`

## 2) Task scope
Folder-local RenderingLifecycle cleanup for lights/probes only. No lighting math changes, no render ordering changes, no Stride.Rendering edits, no Dominatus migration.

## 3) Before warnings
- Focused warning count before: **556** (`/tmp/striv-m22o-engine-warning-lines-before.log`)
- Relevant lights/probes warnings before included:
  - `LightProcessor`: `CS8618`, `CS8603`, `CS8620`
  - `LightProbeProcessor`: `CS8618`, `CS8620`
  - `ForwardRenderer.LightProbes`: `CS8602`, `CS8604`
  - `LightProbeComponent`: `CS8618`

## 4) Lights/probes classification table
| File/site | Warning | Pattern | Null possible? | Intended behavior | Action |
|---|---|---|---:|---|---|
| `LightProcessor.VisibilityGroup` | CS8618 | runtime ambient render state | Yes pre-system-attach | uninitialized pre-render, required on add | keep non-null contract, assign `null!`, deterministic throw in `OnSystemAdd` |
| `LightProcessor.OnSystemRemove` | CS8620 | nullable boundary on tag clear | Yes | optional ambient tag removal | use `Tags.Remove(...)` instead of `Set(..., null)` |
| `LightProcessor.GetRenderLight` | CS8603 | missing entry in map | Yes | absence allowed | return `RenderLight?` |
| `LightProbeProcessor.VisibilityGroup` | CS8618 | runtime ambient render state | Yes pre-system-attach | may not exist at construction | keep non-null contract + `null!` init |
| `LightProbeProcessor.UpdateLightProbePositions` | CS8620 | nullable boundary on tag clear | Yes | missing probe runtime data is valid | `VisibilityGroup` guard + `Tags.Remove(...)` |
| `LightProbeComponent.Coefficients` | CS8618 | uninitialized collection | Yes by legacy use | inert default should be safe | initialize to empty list in constructor |
| `ForwardRenderer.LightProbes` depth/pipeline state | CS8602/CS8604 | GPU lifecycle-bound nullable state | Yes before full renderer init | required only in active render path | deterministic exceptions for missing depth-stencil/pipeline state |
| `LightProbeGenerator` | CS8602 | entity/light-probe runtime assumptions | Possible | requires deeper scene/runtime contract pass | deferred |

## 5) Tests
Added `RenderingLightLifecycleTests`:
- `LightProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice`
- `LightProbeProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice`
- `LightProbeComponent_DefaultConstruction_HasValidInertState`

These pin cheap construction/default-state behavior only; GPU/device lifecycle behavior remains deferred.

## 6) Fixes applied
- `LightProcessor`: nullable-return fix (`GetRenderLight`), deterministic `OnSystemAdd` visibility invariant check, and tag-removal semantics in `OnSystemRemove`.
- `LightProbeProcessor`: safe no-op when `VisibilityGroup` is absent, tag-removal semantics, and nullable-safe runtime data read.
- `ForwardRenderer.LightProbes`: deterministic errors for missing depth-stencil and unbuilt pipeline state in render path.
- `LightProbeComponent`: default-initialize `Coefficients` to empty.

## 7) Deferred lights/probes issues
- `LightProbeGenerator` runtime/data-lifecycle nullability (`CS8602`) still present.
- Descriptor optional-slot semantics still rely on render-time resources; partial guard already present with `STRIV-TODO` in `ForwardRenderer.LightProbes`.
- Broader GPU resource lifecycle split remains for later rendering actuator/device lifecycle pass.

## 8) After warnings
- Focused warning count after: **542** (`/tmp/striv-m22o-engine-warning-lines-after.log`)
- Lights/probes local delta: eliminated `LightProcessor` and `LightProbeProcessor` warning lines from focused list.
- Total focused delta: **-14**.

## 9) Next recommendation
Continue with remaining RenderingLifecycle safe chunks first:
1. `Engine/RenderingLifecycle/LightProbes/LightProbeGenerator.cs CS8602` (still directly in current cluster)
2. then `Engine/RenderingLifecycle/Compositing/ForwardRenderer*.cs` nullable guard pass where non-GPU-faked invariants can be asserted.
If these become mostly GPU-lifecycle-bound, pivot to `GameLifecycle/CloneLifecycle` nullable buckets.

## 10) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass, output truncated in terminal capture: yes.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, pass, first meaningful warning: `CS8765` (`CompressedTimeSpan`), output truncated in terminal capture: yes.
- Warning extraction commands (`grep/sed/wc/cat`) used for before/after logs all exited 0 in scripted chains (with `|| true` fallback on empty-grep paths).
