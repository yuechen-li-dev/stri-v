# 1420 — M12k Stride.Games GamePlatform test-first Shine validation

## 1) Files changed
- `striv/projects/Stride.Games/Host/GamePlatform.cs`
- `striv/tests/Stride.Games.Tests/PlatformWindowLifecycleTests.cs`

## 2) Task scope
M12k scope was a test-first Shine pass on `GamePlatform` post-window assumptions in `Stride.Games`, specifically the `CS8602` sites in `GamePlatform.cs` (~352/375/381). No run-loop redesign, backend changes, SDL refactor, or graphics device creation redesign was performed.

## 3) Before warnings
- Focused warning count before: **116**.
- Distribution before:
  - CS8618: 50
  - CS8625: 22
  - CS8602: 12
  - CS8601: 10
  - CS8603: 6
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
- Platform/window filtered warnings before: **48** (includes duplicate lines from summary replay in build output).
- Targeted warning sites before included:
  - `Host/GamePlatform.cs(352,119)` CS8602
  - `Host/GamePlatform.cs(375,188)` CS8602
  - `Host/GamePlatform.cs(381,13)` CS8602

## 4) Fake helpers / tests
- Reused existing test-local `FakeGameWindow` for no-native-window/no-device lifecycle assertions.
- Added test-local probe types in `PlatformWindowLifecycleTests`:
  - `ProbeGamePlatform : GamePlatform` to set/omit `gameWindow` deliberately.
  - `ProbeGameBase : GameBase` minimal abstract implementation only.
- These seams avoid native window creation, SDL loop, and real graphics device/presenter creation.

## 5) Test-first workflow
### Cluster A — before window creation
- Test written first: `GamePlatform_MainWindow_BeforeCreateWindow_ThrowsInvalidOperationException`.
- Initial result: failed (no exception thrown).
- Production change: `MainWindow` now throws `InvalidOperationException` when `gameWindow` is null.
- Final result: passes.

- Test written first: `GamePlatform_Exit_BeforeCreateWindow_DoesNotThrow`.
- Initial result: passed.
- Production change: none for this behavior (kept existing no-op semantics).
- Final result: passes.

### Cluster B/C — post-window graphics-path assumption precondition
- Test written first: `GamePlatform_PostCreateWindow_DeviceChanged_UsesCreatedWindow`.
- Initial result: passed with probe window attached.
- Production change: `CreateDevice`, `RecreateDevice`, and `DeviceChanged` now flow through `MainWindow` local variable/guarded property rather than direct nullable field dereference.
- Final result: passes.

## 6) Tests added
- `GamePlatform_MainWindow_BeforeCreateWindow_ThrowsInvalidOperationException` — locks predictable failure before host window exists.
- `GamePlatform_Exit_BeforeCreateWindow_DoesNotThrow` — locks existing safe no-op behavior before window creation.
- `GamePlatform_PostCreateWindow_DeviceChanged_UsesCreatedWindow` — locks post-create precondition that window-dependent operations require an actual window object.

## 7) Fixes applied
### `striv/projects/Stride.Games/Host/GamePlatform.cs`
- Replaced null-forgiving `MainWindow` accessor with explicit `InvalidOperationException` when window is absent.
- Updated warning sites to use guarded `MainWindow`/local `window` references in:
  - `CreateDevice`
  - `RecreateDevice`
  - `DeviceChanged`
- Rationale: narrows lifecycle precondition to an explicit contract and removes unsafe nullable-field dereferences.
- Coverage: new `PlatformWindowLifecycleTests` additions above.

## 8) Deferred warnings
Deferred as out of M12k scope:
- Full `GameBase` run-loop warning cleanup.
- Real backend/message loop behavior under native platform hosts.
- Graphics device/presenter construction lifecycles requiring real runtime backends.
- SDL backend internals.
- Remaining non-GamePlatform platform/window warnings (mostly `GameBase`, SDL, and other subsystems).

## 9) After warnings
- Focused warning count after: **110**.
- Distribution after:
  - CS8618: 50
  - CS8625: 22
  - CS8601: 10
  - CS8603: 6
  - CS8602: 6
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
- Platform/window filtered warnings after: **42** (duplicate replay included).
- Focused checker status: **4** (warnings remain), as expected.
- Delta from M12j baseline (116): **-6** warnings.

## 10) Validation results
- `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
  - exit 0, pass, first warning `GameBase.cs(419,51) CS8625`, output truncated: no.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - pre-change run exit 1 (intentional red phase), first failure: `GamePlatform_MainWindow_BeforeCreateWindow_ThrowsInvalidOperationException` no exception thrown, output truncated: no.
  - post-change run exit 0, pass, output truncated: no.
- `./striv/build/striv-check-focused-project.sh Stride.Games`
  - exit 4, expected focused-warning gate failure, output truncated: no.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
  - exit 0, pass, first warning `GameBase.cs(419,51) CS8625`, output truncated: yes (terminal cap).
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` exit 0, pass.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` exit 0, pass.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` exit 0, pass.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` exit 0, pass (1 skipped test).
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` exit 0, pass.
- `./striv/build/striv-build-core.sh` exit 0, pass.

## 11) Recommended next task
Another targeted lifecycle Shine pass: prioritize `GameBase` host lifecycle nullability assumptions (non-window run-loop-adjacent areas still generating repeated focused warnings), keeping test-first discipline and preserving backend boundaries.
