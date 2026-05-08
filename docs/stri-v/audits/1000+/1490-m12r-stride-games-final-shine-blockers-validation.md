# 1490 — M12r Stride.Games final shine blockers validation

## 1) Files changed
- striv/projects/Stride.Games/GameContextSDL.cs
- striv/projects/Stride.Games/GameContextWinforms.cs
- striv/projects/Stride.Games/Host/GamePlatform.cs
- striv/projects/Stride.Games/Desktop/GamePlatformDesktop.cs
- striv/projects/Stride.Games/GraphicsBridge/GameWindowRenderer.cs
- striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceManager.cs
- striv/projects/Stride.Games/SDL/GameWindowSDL.cs

## 2) Task scope
This pass executed the pre-standardization blocker sweep for `Stride.Games` to remove the remaining focused warning buckets (18 focused lines from M12q) where safe.
No warning suppression pragmas/NoWarn were used.

## 3) Before warnings
- Total focused warning lines before: **18** (`wc -l /tmp/striv-m12r-games-warning-lines-before.log`).
- Distribution before:
  - CS8625 × 6
  - CS0162 × 6
  - CS8618 × 2
  - CS8603 × 2
  - CS8602 × 2
- Buckets before:
  - GraphicsBridge/GraphicsDeviceManager.cs CS0162 × 6
  - Host/GameContextFactory.cs CS8625 × 4
  - SDL/GameWindowSDL.cs CS8625 × 2
  - GraphicsBridge/GraphicsDeviceManager.cs CS8618 × 2
  - GraphicsBridge/GameWindowRenderer.cs CS8602 × 2
  - Desktop/GamePlatformDesktop.cs CS8603 × 2

## 4) Per-bucket decision table
| Bucket | Initial count | Resolution attempted | Tests added/used | Result | Remaining? |
| ------ | ------------: | -------------------- | ---------------- | ------ | ---------- |
| GameContextFactory CS8625 | 4 | Made `GameContextSDL` and `GameContextWinforms` ctor `control` params nullable to match null sentinel factory calls. | Existing `Stride.Games.Tests` lifecycle suite. | Resolved. | No |
| GamePlatformDesktop CS8603 | 2 | Updated `GetSupportedGameWindow` contract to nullable in base + override (`GameWindow?`). | Existing `Stride.Games.Tests` lifecycle/platform tests. | Resolved. | No |
| GameWindowSDL CS8625 | 2 | Replaced destroy-time null assignment with `default!` reset to preserve post-destroy sentinel without nullable-flow churn. | Existing `Stride.Games.Tests` lifecycle suite. | Resolved. | No |
| GraphicsDeviceManager CS8618 | 2 | Explicit constructor initialization (`GraphicsDevice = null!`) for delayed device lifecycle contract. | Existing `Stride.Games.Tests` lifecycle suite. | Resolved. | No |
| GameWindowRenderer CS8602 | 2 | Added explicit service precondition (`IGamePlatform` required) before use in `Initialize`. | Existing `GameWindowRendererLifecycleTests`. | Resolved. | No |
| GraphicsDeviceManager CS0162 | 6 | Removed compile-time unreachable `else` branches guarded by constant `DelayWindowEvents = true`. | Build-time verification. | Resolved. | No |

## 5) Tests added/updated
No new tests were required; this pass was contract/flow cleanup and compile-time branch-shape cleanup only.
Existing tests used:
- `Stride.Games.Tests` lifecycle and renderer lifecycle coverage.

No native window backend or graphics device creation was introduced by this pass.

## 6) Fixes applied
- `GameContextSDL.cs` / `GameContextWinforms.cs`: nullable constructor control input to align with existing factory sentinel calls.
- `GamePlatform.cs` + `GamePlatformDesktop.cs`: nullable `GetSupportedGameWindow` contract alignment for unsupported context return sentinel.
- `GameWindowRenderer.cs`: explicit null-guard for missing `IGamePlatform` service before dereference.
- `GraphicsDeviceManager.cs`: explicit delayed initialization for `GraphicsDevice`, and removal of dead compile-time unreachable event branches.
- `GameWindowSDL.cs`: destroy path assignment adjusted to avoid nullability warning while preserving lifecycle semantics.

## 7) Unresolved warnings
None in focused `Stride.Games` build.

## 8) After warnings
- Total focused warning lines after: **0** (`wc -l /tmp/striv-m12r-games-warning-lines-after.log`).
- Distribution after: none.
- Focused checker exit status: `0`.
- `Stride.Games` is ready for **Standardize/Sustain**.

## 9) Validation results
All required validation commands were executed and passed (exit code 0):
- Focused project build and focused warning extraction (before/after).
- `./striv/build/striv-check-focused-project.sh Stride.Games` (after: status 0).
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`.
- `dotnet test` for:
  - `Stride.Games.Tests`
  - `Stride.Input.Tests`
  - `StriV.AssetTool.Tests`
  - `StriV.AssetPipeline.Tests` (`--no-build`)
  - `StriV.ShaderPipeline.Tests` (`--no-build`, 1 skipped test)
  - `StriV.CleanGraph.Tests`
- `./striv/build/striv-build-core.sh`

No command output required truncation for blocker diagnosis.

## 10) Recommended next task
Proceed with **M12s Standardize/Sustain for `Stride.Games`**.
