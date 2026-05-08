# 1470 — M12p Stride.Games GameContext Shine Validation

## 1) Files changed
- `striv/projects/Stride.Games/Host/GameContext.cs`
- `striv/tests/Stride.Games.Tests/LifecycleTests.cs`

## 2) Task scope
Focused on `Host/GameContext.cs` constructor/property contract nullability warnings (`CS8618`) only. No native window loop, SDL runtime, graphics device creation path, or backend/platform behavior changes were made.

## 3) Before warnings
- Focused warning count before: **56** (`/tmp/striv-m12p-games-warning-lines-before.log`).
- Distribution before:
  - CS8618: 14
  - CS8625: 12
  - CS8602: 6
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
  - CS8603: 4
  - CS8601: 4
- `Host/GameContext*` lines before: **10** (duplicated by build log summary), with `GameContext.cs` sites at lines 55, 61, 88 and `GameContextFactory.cs` at 80, 109.

## 4) GameContext contract map
- Required constructor values: generic `control`, `requestedWidth`, `requestedHeight`, `isUserManagingRun` are assigned in `GameContext<TK>` constructor.
- Optional/null values:
  - `RunCallback` and `ExitCallback` are legitimately absent before user-managed run wiring.
  - Headless control is intentionally `null` via `GameContextHeadless : GameContext<object?>`.
- Backend-owned values:
  - `RequestedGraphicsProfile` may be absent prior to graphics setup.
- Headless behavior:
  - `ContextType = Headless`, no native control requirement, dimensions preserved.

## 5) Test-first workflow
### Cluster A/B/C (combined due API shape)
1. Added contract tests first in `LifecycleTests.cs` for headless defaults and factory-headless behavior.
2. Ran tests before production edits (`dotnet test ...Stride.Games.Tests...`) — pass.
3. Applied production nullability contract changes in `GameContext.cs`.
4. Re-ran tests — pass.
5. Rebuilt focused project and measured warning delta.

## 6) Tests added
- `GameContextHeadless_Defaults_AreStableForContract`
  - Locks defaults: headless type, zero dimensions, null control, null callbacks, null requested graphics profile.
- `GameContextFactory_HeadlessContext_UsesRequestedDimensions_WithoutNativeControl`
  - Locks factory behavior for headless path and requested dimensions without native control/window dependency.

## 7) Fixes applied
- `GameContext.cs`
  - `RunCallback` and `ExitCallback` changed to nullable `Action?`.
  - `RequestedGraphicsProfile` changed to nullable `GraphicsProfile[]?`.
  - Rationale: these members are legitimately unset at construction and populated later by lifecycle/platform flow.
- `LifecycleTests.cs`
  - Added explicit contract tests to verify pre-run null/default states are intentional and stable.

## 8) After warnings
- Focused warning count after: **48** (`/tmp/striv-m12p-games-warning-lines-after.log`).
- Distribution after:
  - CS8625: 12
  - CS8618: 8
  - CS8602: 6
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
  - CS8603: 4
  - CS8601: 2
- `Host/GameContext*` lines after: **4** (only `GameContextFactory.cs` duplicated lines at 80, 109).
- Focused checker status: **4** (warnings remain).
- Delta from M12o baseline (56): **-8** warnings.

## 9) Remaining warning bucket analysis
Top remaining buckets (`sed ... | head -n 40`):
- `GraphicsBridge/GraphicsDeviceManager.cs CS0162` (6)
- `Systems/GameSystemCollection.cs CS8604` (4)
- `Systems/GameSystemCollection.cs CS8600` (4)
- `SDL/GameWindowSDL.cs CS8618` (4)
- `Host/GameContextFactory.cs CS8625` (4)
- `GraphicsBridge/GraphicsDeviceManager.cs CS8625` (4)
- `Desktop/GamePlatformDesktop.cs CS8603` (4)

Recommended next bucket: `Host/GameContextFactory.cs CS8625` (small, contiguous, same subsystem proximity, likely testable without native runtime).

## 10) Validation results
Recorded commands executed with observed outcomes:
- `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` → exit 0, warnings present, output not truncated.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (before changes) → exit 0, pass, not truncated.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (after changes) → exit 0, pass, not truncated.
- `./striv/build/striv-check-focused-project.sh Stride.Games` → exit 4 expected (warnings remain), not truncated.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` → exit 0, focused warnings in Stride.Games, not truncated.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` → exit 0.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` → exit 0.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` → exit 0.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` → exit 0, one pre-existing skipped test.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` → exit 0.
- `./striv/build/striv-build-core.sh` → exit 0.

## 11) Recommended next task
Proceed with focused M12q on `Host/GameContextFactory.cs` nullable-control constructor calls (`CS8625`) as the next low-risk, high-confidence bucket.
