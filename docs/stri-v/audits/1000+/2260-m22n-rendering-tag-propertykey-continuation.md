# 2260 — M22n render-tag / PropertyKey boundary continuation

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/SceneLifecycle/SceneInstance.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/SceneCameraSlotCollection.cs`
- `docs/stri-v/audits/1000+/2260-m22n-rendering-tag-propertykey-continuation.md`

## 2) Task scope
This pass continued the local RenderingLifecycle tag-contract cleanup around render-context ambient tags only.
No render ordering/pipeline logic was rewritten, no scene lifecycle rewrite was performed, and no Dominatus migration work was introduced.

## 3) Before warnings
- Focused warning count before: **566** (`wc -l /tmp/striv-m22n-engine-warning-lines-before.log`).
- Relevant local boundary warnings included:
  - `GraphicsCompositor.cs(181,64)` CS8620 on `SceneInstance.CurrentVisibilityGroup`
  - `GraphicsCompositor.cs(182,64)` CS8620 on `SceneInstance.CurrentRenderSystem`
  - `GraphicsCompositor.cs(183,64)` CS8620 on `SceneCameraSlotCollection.Current`
- Top code bucket snapshot (before): CS8618, CS8625, CS8602, CS8604, CS8603, CS8601, CS8600, CS8622.

## 4) PropertyKey/tag findings
- `SceneInstance.CurrentVisibilityGroup` was declared `PropertyKey<VisibilityGroup>` but used with `PushTagAndRestore(..., visibilityGroup)` where value is nullable in non-scene/non-active paths.
- `SceneInstance.CurrentRenderSystem` was declared `PropertyKey<RenderSystem>` but treated as ambient tag value (can be absent outside compositor draw).
- `SceneCameraSlotCollection.Current` was declared `PropertyKey<SceneCameraSlotCollection>` but is an ambient render-context tag that can be absent outside active compositor usage.
- `SceneInstance.GetCurrent(RenderContext)` returned non-nullable `SceneInstance` while backing lookup may return null.
- `PushTagAndRestore<T>(PropertyKey<T?> key, T value)` expects nullable-key contract for reference ambient tags.

## 5) Classification table
| File/site | Warning | Key/value type | Null possible? | Intended tag behavior | Action |
| --- | --- | --- | ---: | --- | --- |
| `SceneInstance.CurrentVisibilityGroup` | CS8620 | `PropertyKey<VisibilityGroup>` | Yes | Ambient current visibility group is optional outside active scene/compositor draw | Changed to `PropertyKey<VisibilityGroup?>` |
| `SceneInstance.CurrentRenderSystem` | CS8620 | `PropertyKey<RenderSystem>` | Yes | Ambient current render system is optional outside active draw path | Changed to `PropertyKey<RenderSystem?>` |
| `SceneCameraSlotCollection.Current` | CS8620 | `PropertyKey<SceneCameraSlotCollection>` | Yes | Ambient camera slot collection can be absent when no active compositor context | Changed to `PropertyKey<SceneCameraSlotCollection?>` |
| `SceneInstance.GetCurrent(RenderContext)` | CS8603 linkage | returned `SceneInstance` | Yes | Caller may have no active scene tag | Changed return type to `SceneInstance?` |

## 6) Tests
No new tests were added in this pass.
Reason: this was a local nullable-contract alignment with existing ambient tag semantics and no behavior/path ordering changes.
Existing engine and solution tests were run to validate no regressions.

## 7) Fixes applied
- `SceneInstance` local key contracts were aligned to nullable reference semantics for ambient render tags.
- `SceneInstance.GetCurrent(RenderContext)` now returns nullable, matching tag lookup semantics.
- `SceneCameraSlotCollection.Current` key contract changed to nullable reference generic.

Behavior remains unchanged: this pass only aligns type contracts with already-optional runtime usage and removes boundary mismatches at callsites.

## 8) Deferred tag/render issues
- Remaining `CS8620` buckets in rendering family remain in LightProbe and LightProcessor clusters.
- Device/render-graph dependent semantics were intentionally deferred.
- Light-probe descriptor optional-slot semantics remain next recommended rendering nullability cluster.

## 9) After warnings
- Focused warning count after: **556** (`wc -l /tmp/striv-m22n-engine-warning-lines-after.log`).
- Tag-boundary warnings in this cluster (`GraphicsCompositor.cs` lines 181-183 before) were cleared.
- Total focused delta: **-10**.

## 10) Next recommendation
From `/tmp/striv-m22n-engine-warning-buckets-after.log`, prioritize remaining rendering-lifecycle CS8620 buckets:
1. `Engine/RenderingLifecycle/LightProbes/LightProbeProcessor.cs CS8620`
2. `Engine/RenderingLifecycle/Lights/LightProcessor.cs CS8620`
Then proceed to light-probe optional-slot/descriptor semantics.

## 11) Validation results
Commands executed, status summary:
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, pass, warnings expected baseline/after snapshots, output truncated in terminal capture: yes.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass, 44 passed, output truncated: yes.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` → exit 0, pass, first meaningful warning `RS1036` in serialization generator project, output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` → exit 0, pass all projects, output truncated: no (summary visible).
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` → exit 0, pass (80/80), output truncated: yes.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` → exit 0, pass (15/15), first warning `CS0618` old descriptor compatibility tests, output truncated: no.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` → exit 0, pass (25/25), output truncated: no.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` → exit 0, pass (10/10), output truncated: no.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` → exit 0, pass (4/4), output truncated: no.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` → exit 0, pass (5/5), output truncated: no.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass (11/11), output truncated: no.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass (66 passed, 1 skipped), output truncated: no.
- `./striv/build/striv-build-core.sh` → exit 0, pass, build succeeded, output truncated: yes.
