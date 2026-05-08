# 1390 — M12h Stride.Games GraphicsBridge/Window test-first Shine validation

## 1) Files changed
- striv/tests/Stride.Games.Tests/GraphicsBridgeLifecycleTests.cs
- striv/projects/Stride.Games/GraphicsBridge/GameGraphicsParameters.cs
- striv/projects/Stride.Games/GraphicsBridge/GameWindowRenderer.cs
- striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceManager.cs

## 2) Task scope
M12h scope was a test-first Shine pass on Stride.Games GraphicsBridge/window lifecycle warning clusters.
All production edits were made after tests existed and were executed.
No real graphics device, native window, SDL loop, or run-loop behavior refactor was introduced.

## 3) Before warnings
- Focused warning count before: **190** (`/tmp/striv-m12h-games-warning-lines-before.log`).
- Distribution before:
  - CS8618: 112
  - CS8625: 40
  - CS8601: 8
  - CS8604: 6
  - CS8603: 6
  - CS8602: 6
  - CS0162: 6
  - CS8600: 4
  - CS8073: 2
- Bridge/window warning lines before: **142** (`/tmp/striv-m12h-bridge-window-warnings-before.log`).

## 4) Test-first workflow
### Cluster A: GameGraphicsParameters nullable/default lifecycle
1. Test written first: `GameGraphicsParameters_Constructs_WithStableDefaults`.
2. Test run: failed (PreferredGraphicsProfile was null).
3. Production change: initialized `PreferredGraphicsProfile` to empty array default.
4. Test rerun: passed.

### Cluster B: GameWindowRenderer pre-initialize lifecycle nullability
1. Test written first: `GameWindowRenderer_Constructs_WithoutPresenter`.
2. Test run: guard existed and executed.
3. Production change: marked `Window`, `Presenter`, and `savedPresenter` nullable for pre-initialize/destroy lifecycle.
4. Test rerun: passed.

### Cluster C: GraphicsDeviceInformation stable construction
1. Test written first: `GraphicsDeviceInformation_Constructs_WithStableDefaults`.
2. Test run: guard existed and executed.
3. Production change: none needed (behavior already stable).
4. Test rerun: passed.

### Cluster D: GraphicsDeviceManager no-subscriber lifecycle events
1. Tests already in place around lifecycle area; no behavior-sensitive constructor path test seam for full manager without game host.
2. Production change limited to nullable event and adapter UID annotations to match valid no-subscriber / unset-adapter lifecycle.
3. Focused build confirms warning reduction and no API-flow redesign.

## 5) Tests added
- `GameGraphicsParameters_Constructs_WithStableDefaults`
  - Locks expected default/absent lifecycle values including non-null profile collection.
  - Supports safe nullability cleanup of DTO defaults.
  - No native graphics/window dependency.
- `GraphicsDeviceInformation_Constructs_WithStableDefaults`
  - Locks constructor stability for adapter/presentation defaults.
  - Prevents accidental lifecycle regressions in bridge config data.
  - No real graphics device creation.
- `GameWindowRenderer_Constructs_WithoutPresenter`
  - Locks pre-Initialize contract that renderer has context but no window/presenter yet.
  - Justifies nullable lifecycle annotations in renderer.
  - Uses headless context only.

## 6) Fixes applied
- `GameGraphicsParameters`
  - Addressed CS8618 on `PreferredGraphicsProfile` by defaulting to `[]`.
  - Behavior safety: preserves “no preferred profiles explicitly set” semantics without null-state ambiguity.
  - Covered by `GameGraphicsParameters_Constructs_WithStableDefaults`.
- `GameWindowRenderer`
  - Addressed constructor-lifecycle CS8618 for `Window`, `Presenter`, `savedPresenter` through nullable annotations.
  - Behavior safety: renderer is legitimately unbound before `Initialize()` and after `Destroy()`.
  - Covered by `GameWindowRenderer_Constructs_WithoutPresenter`.
- `GraphicsDeviceManager`
  - Addressed constructor/no-subscriber CS8618 warnings for events and optional adapter id via nullable annotations.
  - Behavior safety: no-subscriber and unset adapter are existing valid lifecycle states.
  - Covered indirectly by focused build and existing lifecycle expectations; no device lifecycle redesign.

## 7) Deferred warnings
Deferred in M12h due to required runtime/backend seams not present in deterministic unit tests:
- Real graphics/presenter ownership and swapchain lifecycle (`GameWindowRenderer` CS8602/CS8604 hotspots).
- Full graphics device lifecycle requiring concrete platform factory and device creation sequencing.
- Real/native window + SDL message-loop paths.
- Broader `GameBase` and `GamePlatform` run-loop lifecycle null-flow.

## 8) After warnings
- Focused warning count after: **178** (`/tmp/striv-m12h-games-warning-lines-after.log`).
- Distribution after:
  - CS8618: 92
  - CS8625: 36
  - CS8602: 16
  - CS8601: 10
  - CS8604: 6
  - CS8603: 6
  - CS0162: 6
  - CS8600: 4
  - CS8073: 2
- Bridge/window warning lines after: **130** (`/tmp/striv-m12h-bridge-window-warnings-after.log`).
- Focused checker exit status: **4** (warnings remain, expected).
- Delta from M12g 190 baseline: **-12** warnings.

## 9) Validation results
(See shell history and logs under `/tmp/striv-m12h-*` and focused checker output.)
- `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
  - exit 0; warnings present; output truncated in terminal capture (full in log).
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - initial run exit 1 (expected red phase); later runs exit 0.
- `./striv/build/striv-check-focused-project.sh Stride.Games`
  - exit 4; focused warning gate failed with 178 warnings.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
  - exit 0; warnings in focused project remain.
- test suites (`Stride.Games.Tests`, `Stride.Input.Tests`, `StriV.AssetTool.Tests`, `StriV.AssetPipeline.Tests`, `StriV.ShaderPipeline.Tests`, `StriV.CleanGraph.Tests`) all exit 0.
- `./striv/build/striv-build-core.sh` exit 0.

## 10) Recommended next task
Next targeted pass: add deterministic seams/tests for `GameWindowRenderer` presenter lifecycle transitions (pre-init / initialized / destroy) using a fake `IGamePlatform` + fake `GameWindow`, then reduce CS8602/CS8604 in renderer without changing presenter ownership semantics.
