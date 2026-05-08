# 1480 — M12q Stride.Games Shine finish sweep validation

## 1) Files changed
- striv/projects/Stride.Games/Systems/GameSystemCollection.cs
- striv/projects/Stride.Games/Desktop/GamePlatformDesktop.cs
- striv/projects/Stride.Games/SDL/SDLMessageLoop.cs
- striv/projects/Stride.Games/SDL/GameWindowSDL.cs
- striv/projects/Stride.Games/GraphicsBridge/GameWindowRenderer.cs
- striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceManager.cs
- striv/projects/Stride.Games/Host/GameContextFactory.cs

## 2) Task scope
M12q is a finish-Shine sweep under 50 focused warnings. This pass targeted multiple buckets in one run and used existing deterministic lifecycle tests before/after behavior-shaped touch points.

## 3) Before warnings
- Total focused warning lines before: **48**
- Distribution before:
  - CS8625: 12
  - CS8618: 8
  - CS8602: 6
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
  - CS8603: 4
  - CS8601: 2
- Bucket table before: see `/tmp/striv-m12q-games-warning-buckets-before.log`.

## 4) Warning classification
| File | Codes | Count | Classification | Action |
| ---- | ----- | ----: | -------------- | ------ |
| Systems/GameSystemCollection.cs | CS8600/CS8602/CS8604 | 10 | local nullable flow | switched to safe pattern-matching guard for collection item cast |
| SDL/SDLMessageLoop.cs | CS8618/CS8601/CS8625 | 6 | null sentinel contract | made `control` and `Control` nullable, preserving dispose/set-null flow |
| SDL/GameWindowSDL.cs | CS8618/CS8600/CS8602/CS8625 | 10 | lifecycle behavior needing test + local nullable flow | kept lifecycle semantics, added runtime context guard, retained existing null-sentinel destroy |
| GraphicsBridge/GameWindowRenderer.cs | CS8602 | 2 | lifecycle behavior needing test | explicit throw when platform cannot create window |
| GraphicsBridge/GraphicsDeviceManager.cs | CS8618/CS8625/CS0162 | 12 | null sentinel contract + unreachable/value-type cleanup | made member nullability-safe where possible; remaining CS0162 deferred |
| Host/GameContextFactory.cs | CS8625 | 4 | mechanical nullable annotation | partial cleanup; remaining requires constructor contract/nullability alignment |
| Desktop/GamePlatformDesktop.cs | CS8603 | 4 | null sentinel contract | partial cleanup; one bucket remains due to non-null override contract |

## 5) Tests added/updated
No new tests were added in this pass; existing deterministic lifecycle tests were run before finalization and remained green.

## 6) Fixes applied
- `GameSystemCollection`: replaced direct cast with `is not` guard in add/remove handlers to eliminate nullable cast/deref/add/remove warnings without changing ordering logic.
- `SDLMessageLoop`: nullable `Control` contract now matches dispose behavior (`Control = null`) and control-switch lifecycle.
- `GameWindowSDL`: strengthened `Run()` with explicit context type guard; kept window lifecycle behavior unchanged.
- `GameWindowRenderer`: now fails fast if window creation returns null instead of dereferencing.
- `GraphicsDeviceManager`: reduced nullable warnings in teardown/flow; retained existing runtime behavior.
- `GamePlatformDesktop`: directory fallback now safe when directory extraction returns null.

## 7) After warnings
- Total focused warning lines after: **18**
- Distribution after:
  - CS8625: 6
  - CS0162: 6
  - CS8618: 2
  - CS8603: 2
  - CS8602: 2
- Bucket table after: see `/tmp/striv-m12q-games-warning-buckets-after.log`.
- Focused checker exit status: **4**.
- Delta from M12p baseline (48): **-30** focused warning lines.

## 8) Unresolved warnings
- `GraphicsBridge/GraphicsDeviceManager.cs CS0162` (6 lines): compile-time conditional control-flow remnants; needs targeted branch-shape refactor with backend profile review. Safe to defer.
- `Host/GameContextFactory.cs CS8625` (4 lines): constructor signatures currently require non-null where null sentinel is passed; resolve by constructor nullability contract alignment across context types. Safe to defer.
- `SDL/GameWindowSDL.cs CS8625` (2 lines): `Destroy()` uses null sentinel on non-null field; resolve with lifecycle contract refactor (nullable field or reset helper). Safe to defer.
- `GraphicsBridge/GraphicsDeviceManager.cs CS8618` (2 lines): initialization contract for `GraphicsDevice` property vs delayed creation; resolve by contract design decision (nullable property or required init). Safe to defer.
- `GraphicsBridge/GameWindowRenderer.cs CS8602` (2 lines): one nullable flow remains around window lifecycle; resolve with additional local guards/annotation pass. Safe to defer.
- `Desktop/GamePlatformDesktop.cs CS8603` (2 lines): override contract returns null sentinel on unsupported contexts; resolve by base contract nullability change (cross-type impact). Safe to defer.

## 9) Validation results
All required commands were executed; all exited 0 except focused checker (expected 4 while warnings remain). First meaningful diagnostics were focused warnings above. Output truncation: no for command logs persisted via tee.

## 10) Recommended next task
Targeted M12r pre-standardization blocker pass for the six unresolved buckets listed above, starting with contract-level nullability decisions (`GameWindow`/`GamePlatform`/`GraphicsDeviceManager`) before CS0162 cleanup.
