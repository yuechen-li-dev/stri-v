# 1400 - M12i Stride.Games GameWindowRenderer test-first Shine validation

## 1) Files changed
- striv/projects/Stride.Games/GraphicsBridge/GameWindowRenderer.cs
- striv/tests/Stride.Games.Tests/GameWindowRendererLifecycleTests.cs
- striv/tests/Stride.Games.Tests/Fakes/FakeGameWindow.cs
- striv/tests/Stride.Games.Tests/Fakes/FakeGamePlatform.cs

## 2) Task scope
M12i performed a test-first lifecycle Shine pass on `GameWindowRenderer` presenter/window transitions (pre-init, initialize/bind, destroy), using test-local fake host/window helpers and no native window/device creation.

## 3) Before warnings
- Focused warning count before: **178**.
- Distribution before:
  - CS8618: 92
  - CS8625: 36
  - CS8602: 16
  - CS8601: 10
  - CS8604: 6
  - CS8603: 6
  - CS0162: 6
  - CS8600: 4
  - CS8073: 2
- Renderer/bridge/window filtered lines before: **98** (included repeated lines from build output); `GameWindowRenderer` had multiple CS8602/CS8604/CS8601 dereferences around initialize/presenter/draw lifecycle.

## 4) Fake test helpers
- Added `FakeGameWindow` and `FakeGamePlatform` under `Stride.Games.Tests/Fakes`.
- They are test-only to isolate lifecycle behavior without changing public production API.
- Simulate only host `CreateWindow` binding, basic window state, and disposal accounting.
- Deliberately avoid native handles, SDL loops, and real graphics device creation.

## 5) Test-first workflow
Cluster A:
- Wrote lifecycle tests first for initialize, destroy, and pre-init begin draw behavior.
- Initial red phase: helper compile gaps (`SetTitle` implementation and `WindowHandle` construction).
- Adjusted fake helpers (test-only).
- Re-ran tests to green.

Cluster B:
- With lifecycle expectations locked, updated `GameWindowRenderer` nullability flow/guards in tested lifecycle paths.
- Re-ran tests and focused build.

## 6) Tests added
- `GameWindowRenderer_Initialize_BindsWindowWithoutGraphicsDevice`
  - Locks host/window binding semantics and visible flag assignment.
- `GameWindowRenderer_Destroy_ClearsPresenterAndWindow_AndIsIdempotent`
  - Locks destroy null clearing and safe repeated dispose behavior.
- `GameWindowRenderer_BeginDraw_BeforeInitialize_ReturnsFalse`
  - Locks pre-init draw contract without forcing graphics/native dependencies.

## 7) Fixes applied
`GameWindowRenderer.cs`:
- Added explicit guard flow for lifecycle-sensitive references (`Window`, `GraphicsDevice`, `Presenter`) in size/presenter creation and draw/end-draw paths.
- Replaced nullable-dereference sites with guarded local flow and explicit `InvalidOperationException` for invalid lifecycle usage.
- Preserved presenter/window ownership semantics and draw ordering.

## 8) Deferred warnings
Deferred as out-of-slice:
- Real graphics device lifecycle wiring.
- Real window/message loop behavior.
- SDL backend specifics.
- `GameBase` run-loop level warning buckets.
- Remaining non-targeted `GameWindow`/`GamePlatform` warning set.

## 9) After warnings
- Focused warning count after: **164**.
- Distribution after:
  - CS8618: 92
  - CS8625: 36
  - CS8601: 8
  - CS8603: 6
  - CS8602: 6
  - CS0162: 6
  - CS8604: 4
  - CS8600: 4
  - CS8073: 2
- Renderer/bridge/window filtered lines after: **84**.
- Focused checker exit status: **4** (expected while warnings remain).
- Delta from M12h baseline: focused warnings improved from 178 to 164 in this slice.

## 10) Validation results
- `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` -> exit 0, warnings present, pass (log captured).
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` -> exit 0, pass.
- `./striv/build/striv-check-focused-project.sh Stride.Games` -> exit 4, focused warning gate failure expected.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` -> exit 0, pass.
- Additional requested test/build commands executed in sequence and completed with exit 0.
- Output truncation: yes for long combined command logs in terminal capture; raw log files were still produced where requested.

## 11) Recommended next task
Proceed with another targeted lifecycle test/Shine pass for `Host/GamePlatform` and `Windowing/GameWindow` constructor/event nullability buckets (large CS8618 concentration), keeping no-native seams and deterministic tests.
