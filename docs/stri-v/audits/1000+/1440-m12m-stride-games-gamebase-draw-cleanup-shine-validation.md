# M12m — Stride.Games GameBase draw/cleanup test-first Shine validation

## 1. Files changed
- `striv/projects/Stride.Games/Host/GameBase.cs`
- `striv/tests/Stride.Games.Tests/PlatformWindowLifecycleTests.cs`

## 2. Task scope
M12m targeted a test-first Shine pass for remaining `GameBase` draw/cleanup nullable-flow warnings in `Stride.Games`, without run-loop redesign or graphics/native backend changes. Incidental cleanup of test-project `CS8603` in `PlatformWindowLifecycleTests.cs` was included because it was trivial and local.

## 3. Before warnings
- Focused warning lines before (duplicated by compiler summary): **84**.
- Distribution before:
  - CS8618: 26
  - CS8625: 20
  - CS8602: 14
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
  - CS8603: 4
  - CS8601: 4
- `Host/GameBase` warning lines before: **16** (8 unique x2 from summary), including `CS8602` and `CS8625` in draw/cleanup/event paths.
- Test project warning observed in prior reports: `PlatformWindowLifecycleTests.cs(56,54) CS8603`.

## 4. Test-first workflow
### Cluster A — cleanup/dispose before init
- Test written first: `GameBase_DisposeBeforeRun_ThrowsInvalidOperationExceptionUntilWindowExists`.
- Initial result: confirmed deterministic exception path when disposing before window creation.
- Production change: none required for this behavior lock (kept current contract).
- Final result: passes with explicit exception assertion.

### Cluster D — graphics-device event handler null-flow before setup
- Test written first: `GameBase_GraphicsDeviceEventHandlers_BeforeSetup_DoNotThrow`.
- Initial result: failed (NullReferenceException via `GraphicsDeviceService_DeviceCreated` pre-setup path).
- Production changes:
  - guarded `GraphicsDeviceService_DeviceCreated` when `graphicsDeviceService`/`GraphicsDevice` unavailable.
  - made `resumeManager` accesses in `GraphicsDeviceService_DeviceReset` null-safe.
  - made `EndDraw` call to `graphicsDeviceManager.EndDraw` null-safe.
- Final result: test passes; no NullReferenceException.

## 5. Tests added/adjusted
- `GameBase_DisposeBeforeRun_ThrowsInvalidOperationExceptionUntilWindowExists` locks pre-run cleanup behavior and documents current deterministic failure mode.
- `GameBase_GraphicsDeviceEventHandlers_BeforeSetup_DoNotThrow` locks safe no-op behavior for pre-initialization event paths directly tied to nullable-flow warnings.
- `ProbeGameWindow.NativeWindow` test override updated to `null!` with comment to resolve incidental test-project `CS8603` while preserving headless semantics.

## 6. Fixes applied
### `GameBase.cs`
- Added early return guard in `GraphicsDeviceService_DeviceCreated` if graphics device service/device is absent.
- Changed `resumeManager.OnReload/OnRecreate` to null-conditional calls.
- Changed `graphicsDeviceManager.EndDraw(present)` to null-conditional.
- Safety rationale: these are lifecycle precondition guards in pre-init paths; they do not alter successful initialized runtime flow.
- Coverage: `GameBase_GraphicsDeviceEventHandlers_BeforeSetup_DoNotThrow`.

### `PlatformWindowLifecycleTests.cs`
- Added/updated lifecycle tests described above.
- Fixed trivial test warning at native-handle probe override.

## 7. After warnings
- Focused warning lines after: **78**.
- Distribution after:
  - CS8618: 26
  - CS8625: 20
  - CS8602: 8
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
  - CS8603: 4
  - CS8601: 4
- `Host/GameBase` warning lines after: **10** (5 unique x2): lines 99, 367, 741, 985, 1013.
- Focused checker exit status: **4** (warnings remain).
- Delta from M12l focused baseline (84): **-6**.

## 8. Deferred warnings
- Non-GameBase buckets remain in:
  - SDL/backend message loop files,
  - graphics bridge/device manager,
  - desktop platform/window helpers,
  - system collection and context factories.
- Remaining GameBase buckets are now mostly `CS8625` assignment-to-null lifecycle contracts and one `CS8602` at device creation (`CreateDevice` flow), which likely need additional explicit lifecycle contract decisions.

## 9. Validation results
See command log summary from this pass:
- Focused project build/test/analysis commands executed with successful command exit where expected.
- Focused checker returned exit code 4 as expected with residual warnings.
- Standard validation suite commands completed successfully in this environment.
- Output truncation: some long commands were partially truncated in terminal transcript due output limits; key counts/statuses were captured from generated log files.

## 10. Recommended next task
Another targeted lifecycle Shine pass on remaining `GameBase` `CS8625`/`CS8602` contracts (constructor service null, `CreateDevice` preconditions, cleanup nullable assignments), with test-first pinning for any changed pre-run/post-dispose behavior.
