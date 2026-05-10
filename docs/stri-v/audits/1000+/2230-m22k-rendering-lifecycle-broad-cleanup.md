# M22k RenderingLifecycle broad cleanup (folder-local)

## 1) Files changed
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/GraphicsCompositor.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/SceneCameraRenderer.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/ForwardRenderer.LightProbes.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Sprites/SpriteRenderProcessor.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Background/BackgroundRenderProcessor.cs
- striv/tests/Stride.Engine.Tests/RenderingLifecycleConstructionTests.cs
- docs/stri-v/audits/1000+/2230-m22k-rendering-lifecycle-broad-cleanup.md

## 2) Task scope
Broad folder-local RenderingLifecycle nullability cleanup pass under `Engine/RenderingLifecycle`; no render ordering/pipeline rewrite; no Dominatus migration.

## 3) Before warnings
- Focused warning count before: **640**
- RenderingLifecycle-heavy buckets included ForwardRenderer/LightProbes, GraphicsCompositor, SceneCameraRenderer, processor constructors.

## 4) Classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
|---|---|---|---|---|---|
| GraphicsCompositor entry renderers | CS8618/CS8604 | non-null properties not initialized pre-runtime | game/editor/single view links optional until configured | optional compositor connection | make nullable + init guard in InitializeCore |
| SceneCameraRenderer camera/child | CS8618/CS8603 | camera slot and child missing at construction | optional configuration; collect/draw should no-op if absent | camera slot optional binding | make nullable and keep guarded resolution |
| ForwardRenderer.LightProbes transient GPU locals | CS8618/CS8600/CS8604 | device-bound fields/locals default null | valid only after renderer/device init | light probe GPU lifecycle | nullable fields/locals + deterministic guard exceptions |
| SpriteRenderProcessor visibility link | CS8618 | interface property assigned by runtime processor system | runtime-initialized optional before attach | visibility group lifecycle | explicit runtime-initialized property + safe null-conditional use |
| BackgroundRenderProcessor active bg | CS8618/CS8625 | active background unset at construction/teardown | absent until enabled background found | optional component state | make ActiveBackground nullable + guarded visibility ops |

## 5) Tests
Added construction/config tests in `RenderingLifecycleConstructionTests` for:
- GraphicsCompositor default collections + optional renderer links.
- SceneCameraRenderer default optional camera/child state.
- Sprite/Background processors construction without graphics device.

Tests intentionally avoid GPU/device-bound lifecycle behavior.

## 6) Fixes applied
- Made optional compositor entry points nullable and added deterministic `InitializeCore` guard when context is unavailable.
- Marked SceneCameraRenderer camera/child links optional, preserving no-op/diagnostic behavior.
- Updated light probe bake fields and locals to nullable lifecycle state and added explicit initialization invariants.
- Kept render processor behavior unchanged while guarding visibility group interactions and nullable active background state.

## 7) Deferred issues
- ForwardRenderer core draw path still has device-bound `RenderStage`/depth resource invariants.
- Light probe descriptor SRV/tag boundaries still depend on runtime render graph contracts.
- PropertyKey nullability mismatches (CS8620 family) in compositor/camera tags remain.

## 8) After warnings
- Focused warning count after: **602**
- Total delta: **-38**
- RenderingLifecycle improved in constructor/optional-configuration areas; hard GPU/runtime invariants remain.

## 9) Next recommendation
Continue `RenderingLifecycle` with a narrower `ForwardRenderer` stage/resource contract pass (opaque/transparent/depth and tag/propertykey boundaries). It remains high-count and local, with clear remaining buckets and medium risk if kept behavior-preserving.

## 10) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass, output truncated: yes.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, pass with warnings, output truncated: yes.
