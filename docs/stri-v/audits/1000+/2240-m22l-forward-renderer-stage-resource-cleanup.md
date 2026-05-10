# 2240-m22l-forward-renderer-stage-resource-cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/ForwardRenderer.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/ForwardRenderer.LightProbes.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/SceneCameraRenderer.cs`
- `docs/stri-v/audits/1000+/2240-m22l-forward-renderer-stage-resource-cleanup.md`

## 2) Task scope
Focused RenderingLifecycle cleanup in ForwardRenderer stage/resource contracts. No render ordering rewrite, no compositor semantic rewrite, no Dominatus migration.

## 3) Before warnings
- Focused count before: `602`
- ForwardRenderer/RenderingLifecycle related warnings observed in:
  - `ForwardRenderer.cs` (stage/resource nullability and draw-time resource access)
  - `ForwardRenderer.LightProbes.cs` (device/service/pipeline/SRV contract)
  - `SceneCameraRenderer.cs` (camera slot dereference + PropertyKey boundary)
  - `GraphicsCompositor.cs`/`SceneCameraRenderer.cs` PropertyKey mismatch warnings (deferred)

## 4) Rendering stage-resource classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| ForwardRenderer.InitializeCore | CS8602 | `GraphicsDevice.Features` deref | Graphics device must exist after initialization | required-after-load render resource | Added deterministic InvalidOperationException guard |
| ForwardRenderer.DrawView GBuffer draw | CS8604 | Nullable stage passed to draw | GBuffer draw only when configured | required-after-load render stage | Switched to pattern `GBufferRenderStage is RenderStage gbufferRenderStage` |
| ForwardRenderer SSS pass | CS8604 | nullable depth SRV into Draw | SSS requires depth SRV | draw-time render target/depth resource | Added deterministic throw when missing |
| ForwardRenderer transparent pass | CS8604 | temporary SRV nullable release | opaque SRV optional | optional stage/resource link | Release only when non-null |
| ForwardRenderer post-effects | CS8602 | use `OpaqueRenderStage.OutputValidator` | Post FX path expects opaque stage | render pipeline invariant not visible to compiler | Added deterministic throw before use |
| ForwardRenderer depth read-only resolve | CS8604 | nullable cached depth RT parameter | resolver requires non-null cached arg | draw-time depth resource | Pass fallback `depthStencil` when cache not set |
| ForwardRenderer opaque SRV resolve | CS8602 | possible null render target slot access | binding opaque-as-resource requires active RT | draw-time render target resource | Added deterministic throw when slot empty |
| ForwardRenderer.LightProbes init | CS8604/CS8602 | Services/GraphicsDevice/pipeline nullability | light probe pass requires initialized services/device | graphics device lifecycle field | Added deterministic initialization guard |
| ForwardRenderer.LightProbes descriptor writes | CS8604 | nullable resources bound into descriptor set | descriptor set API requires non-null resources | light probe GPU resource lifecycle | Guard and defer with STRIV-TODO marker |
| SceneCameraRenderer.ResolveCamera | CS8602 | access `Camera.Camera` after nullable check | optional camera slot should no-op cleanly | optional camera/scene tag | local `cameraSlot` guard before deref |
| GraphicsCompositor/SceneCameraRenderer tag push | CS8620 | PropertyKey<T> nullable mismatch | optional tag push should be nullable-safe | PropertyKey nullable mismatch | Deferred (API boundary, broader cleanup) |

## 5) Tests
- Existing `ForwardRendererLifecycleTests` and `RenderingLifecycleConstructionTests` retained as construction/config contract coverage.
- No graphics-device fake initialization added.
- Graphics-device-bound behavior remains deferred.

## 6) Fixes applied
- ForwardRenderer now enforces required initialized graphics device and key stage/resource preconditions with deterministic exceptions instead of nullable ambiguity.
- Stage draw call contracts updated to use non-null stage locals in guarded paths.
- Optional temporary resources (opaque SRV) now guarded on release.
- Light probe path now guards required service/device init and avoids invalid descriptor writes when probe resources are absent, with TODO for API-level optional descriptor semantics.
- Scene camera resolution path now uses explicit slot local guard to avoid nullable slot dereference.

## 7) Deferred issues
- PropertyKey/tag nullability boundaries in GraphicsCompositor/SceneCameraRenderer.
- Graphics-device-bound render pipeline invariants that require runtime render harness.
- Light probe descriptor optional-slot semantics (currently TODO-marked).
- Broader rendering actuator boundary refactor (out of scope).

## 8) After warnings
- Focused count after: `576`
- Total delta: `-26`
- RenderingLifecycle/ForwardRenderer-specific warnings reduced in targeted files, with remaining PropertyKey boundary warnings deferred.

## 9) Next recommendation
Next highest-yield safe target: `Engine/RenderingLifecycle/Compositing/GraphicsCompositor.cs` + `SceneCameraRenderer.cs` PropertyKey API-boundary cleanup (CS8620 cluster), followed by `CameraComponentRendererExtensions` key contract typing. This remains folder-local and testable with construction/config tests.

## 10) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` => exit 0, pass.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0, warnings only.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` => exit 0, warnings only.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` => exit 0, all pass.
- Batched test command for Dominatus/Core.Reflection/Games/Input/CleanGraph/AssetTool/AssetPipeline/ShaderPipeline => exit 0; ShaderPipeline has one expected skipped test.
