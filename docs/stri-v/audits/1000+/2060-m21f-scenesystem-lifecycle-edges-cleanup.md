# 2060 — M21f SceneSystem lifecycle nullability edges cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/SceneSystem.cs`
- `striv/tests/Stride.Engine.Tests/SceneSystemLifecycleTests.cs`

## 2) Task scope
Follow-up to M21e, focused only on SceneSystem lifecycle nullability edges. No SceneSystem rewrite, no runtime rewiring, no Dominatus migration.

## 3) Before warnings
- Focused warning lines before: **860** (`/tmp/striv-m21f-engine-warning-lines-before.log`).
- SceneSystem warning lines before: duplicated 2x in focused log:
  - CS8618 at ctor state for `renderContext` / `renderDrawContext`
  - CS8602 in splash/draw lifecycle dereferences
  - CS8620 PropertyKey nullable-boundary mismatches

## 4) Lifecycle classification table
| Site | Warning | Null pattern | Category | Action |
| ---- | ------- | ------------ | -------- | ------ |
| `renderContext` field | CS8618 | initialized in `LoadContent` only | lifecycle-initialized field | made nullable + guarded accessor throwing `InvalidOperationException` |
| `renderDrawContext` field | CS8618 | initialized in `LoadContent` only | lifecycle-initialized field | made nullable + guarded accessor throwing `InvalidOperationException` |
| `RenderTarget` reads in splash/draw | CS8602 | command-list render target may be absent | render resource lifecycle | guard and early return |
| `PushTagAndRestore(GraphicsCompositor.Current, ...)` | CS8620 | `PropertyKey<T>` vs nullable `T?` | PropertyKey nullable boundary | deferred with `STRIV-TODO` marker |
| `PushTagAndRestore(SceneInstance.Current, ...)` | CS8620 | `PropertyKey<T>` vs nullable `T?` | PropertyKey nullable boundary | deferred with same TODO context |

## 5) Tests
Added lightweight constructor-contract tests in `SceneSystemLifecycleTests`:
- `SceneSystem_Constructed_HasNoActiveSceneInstanceBeforeInitialization`
- `SceneSystem_GraphicsCompositor_CanBeCleared`

No fake game runtime created.

## 6) Fixes applied
### SceneSystem.cs
- Old pattern: non-nullable render lifecycle fields initialized post-construction (`LoadContent`).
- New pattern: nullable backing fields with non-null guarded accessors (`RenderContext`, `RenderDrawContext`) used at draw time.
- Added render-target null guards in splash and draw paths.
- Cleared render context fields during `Destroy` to keep lifecycle state explicit.

Behavior rationale: preserves existing runtime lifecycle while making pre-init / post-destroy state explicit and diagnosable.

## 7) Deferred markers
- `SceneSystem.cs`: `STRIV-TODO` added near tag push for `GraphicsCompositor.Current`.
  - Reason: legacy `PropertyKey<T>` nullability boundary mismatch (`CS8620`).
  - Future direction: dedicated render-tag API nullability audit.

## 8) After warnings
- Focused warning lines after: **856** (`/tmp/striv-m21f-engine-warning-lines-after.log`).
- Total focused delta: **-4**.
- SceneSystem delta: removed CS8618 ctor field warnings; remaining CS8602/CS8620 boundaries persist.

## 9) Validation results
All requested commands were run successfully (exit code 0). Long outputs were truncated by terminal capture for readability.

## 10) Recommended next task
Continue SceneSystem, specifically a constrained PropertyKey/tag nullability boundary pass (CS8620 sites), before moving to larger buckets.
