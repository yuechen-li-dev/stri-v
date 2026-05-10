# 2250 — M22m RenderingLifecycle render-tag / PropertyKey boundary cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/GraphicsCompositor.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/CameraComponentRendererExtensions.cs`
- `docs/stri-v/audits/1000+/2250-m22m-rendering-tag-propertykey-cleanup.md`

## 2) Task scope
This pass stayed folder-local to Stride.Engine RenderingLifecycle boundary cleanup. It addressed render-tag / `PropertyKey<T>` nullability contracts in the targeted cluster only, without renderer ordering changes, render graph rewrites, resource faking, or Dominatus migration.

## 3) Before warnings
- Focused warning count before: **576** (`/tmp/striv-m22m-engine-warning-lines-before.log`).
- Relevant pre-change warning lines included:
  - `SceneCameraRenderer.cs` CS8620 on `PushTagAndRestore(...CameraComponentRendererExtensions.Current...)`.
  - `GraphicsCompositor.cs` CS8620 on compositor tag pushes + CS8600 on nullable `VisibilityGroup` local.
  - `CameraComponentRendererExtensions.cs` CS8603 on `GetCurrentCamera` return.
- Dominant focused code bucket included CS8620 in `GraphicsCompositor.cs` (8 entries) and related render-tag contracts.

## 4) PropertyKey/tag findings
- `ComponentBaseExtensions.PushTagAndRestore` expects `PropertyKey<T?>` for reference-typed tag keys.
- `CameraComponentRendererExtensions.Current` was declared as `PropertyKey<CameraComponent>` even though camera can be absent; this caused generic nullability mismatch (CS8620) and a nullable return boundary issue.
- `GraphicsCompositor.Current` similarly participated in a nullable-key push contract and is semantically optional outside draw scope.
- `VisibilityGroup` in `GraphicsCompositor.DrawCore` is validly absent when no current scene instance exists.
- Warnings in this cluster were primarily generic nullability contract mismatches and optional-tag semantics (not changed runtime behavior).

## 5) Classification table

| File/site | Warning | Key/value type | Null possible? | Intended tag behavior | Action |
| --------- | ------- | -------------- | -------------: | --------------------- | ------ |
| `GraphicsCompositor.Current` | CS8620 | `PropertyKey<GraphicsCompositor>` -> `PropertyKey<GraphicsCompositor?>` | Yes | optional ambient compositor tag | changed key type to nullable generic |
| `GraphicsCompositor.DrawCore` `visibilityGroup` local | CS8600 | `VisibilityGroup` local initialized with `null` | Yes | absent when no scene instance | changed local to `VisibilityGroup?` |
| `SceneCameraRenderer` push of current camera | CS8620 | used key type from extension | Yes | camera absent unless slot resolves | fixed by key contract update |
| `CameraComponentRendererExtensions.Current` | CS8620/CS8603 | `PropertyKey<CameraComponent>` + non-null return | Yes | current camera may be missing | changed key to nullable generic and return type to `CameraComponent?` |

## 6) Tests
- Existing construction/config tests were sufficient for this scope.
- Re-ran `Stride.Engine.Tests` after boundary updates to ensure no regressions in lifecycle construction behavior.

## 7) Fixes applied
- `GraphicsCompositor.cs`
  - old: `PropertyKey<GraphicsCompositor>` and non-nullable `VisibilityGroup` local initialized to null.
  - new: `PropertyKey<GraphicsCompositor?>` and `VisibilityGroup?` local.
  - rationale: aligns optional ambient tag and nullable local with actual runtime absence semantics.
- `CameraComponentRendererExtensions.cs`
  - old: `PropertyKey<CameraComponent>` and `GetCurrentCamera` returning non-null `CameraComponent`.
  - new: `PropertyKey<CameraComponent?>` and `CameraComponent?` return.
  - rationale: camera tag is optional by design; contract now honestly models absence and matches push API.

## 8) Deferred tag/render issues
- Remaining render-tag contract mismatch outside current 3-file cluster:
  - `SceneSystem` still has `SceneInstance.Current` CS8620 boundary (`PropertyKey<SceneInstance>` vs nullable push contract).
- Other deferred rendering items still present from prior audit:
  - light probe descriptor optional-slot semantics.
  - broader graphics-device-bound/runtime invariants.

## 9) After warnings
- Focused warning count after: **566** (`/tmp/striv-m22m-engine-warning-lines-after.log`).
- Delta vs before: **-10** focused warnings.
- Cluster outcome:
  - `SceneCameraRenderer` / `CameraComponentRendererExtensions` boundary warnings cleared.
  - `GraphicsCompositor.Current`-specific key mismatch cleared.
  - Remaining `GraphicsCompositor` CS8620 entries are tied to other keys (`SceneInstance.CurrentVisibilityGroup`, `SceneInstance.CurrentRenderSystem`, `SceneCameraSlotCollection.Current`) outside this pass scope.

## 10) Next recommendation
Next highest-value local RenderingLifecycle follow-up:
1. `SceneInstance.CurrentVisibilityGroup` / `CurrentRenderSystem` / `SceneCameraSlotCollection.Current` nullable key contract alignment (small boundary cluster adjacent to this pass).
2. Then `LightProbeProcessor`/light-probe optional-slot semantics (already flagged by CS8620 + prior audit note).

Rationale: high locality, similar risk profile, and likely continued CS8620 reductions without pipeline behavior changes.

## 11) Validation results
Commands executed and outcomes:
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` — exit 0 — pass — first meaningful warning: existing analyzer/nullability warnings in non-target projects — output truncated: no.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` — exit 0 — pass — first meaningful warning: none blocking — output truncated: no.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` — exit 0 — pass.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` — exit 0 — pass.
- `./striv/build/striv-build-core.sh` — exit 0 — pass.
