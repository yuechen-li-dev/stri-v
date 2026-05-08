# M12l Stride.Games GameBase test-first Shine validation

## 1) Files changed
- `striv/projects/Stride.Games/Host/GameBase.cs`
- `striv/tests/Stride.Games.Tests/PlatformWindowLifecycleTests.cs`

## 2) Task scope
This pass targeted only `GameBase` host lifecycle nullability assumptions with test-first steps.
No full run-loop redesign was performed.
No graphics device creation, native window backend internals, SDL loop internals, or presenter/swapchain behavior were changed.

## 3) Before warnings
Command set run as requested against `Stride.Games.csproj`.
- Focused warning lines before: **110**
- Distribution before:
  - CS8618: 50
  - CS8625: 22
  - CS8601: 10
  - CS8603: 6
  - CS8602: 6
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
- `Host/GameBase` warning lines before: **42** (21 unique, duplicated because build summary prints twice)

## 4) Fake/probe helpers
No new standalone fake files were added.
`ProbeGameBase` in existing lifecycle tests was extended with tiny event trigger helpers to assert no-subscriber lifecycle safety.
These helpers simulate only protected lifecycle event dispatch and avoid native window/message loop and graphics creation.

## 5) Test-first workflow
### Cluster A (default construction / pre-run)
- Test written first: `GameBase_Constructs_WithStablePreRunDefaults`.
- Initial result: passed against current behavior, documenting pre-run defaults.
- Production change: aligned nullable declarations with lifecycle (event and internal host fields) without changing run-loop behavior.
- Final result: passed.

### Cluster C (exit/dispose-adjacent pre-run path)
- Test written first: `GameBase_Exit_BeforeRun_SetsIsExiting_AndDoesNotThrow`.
- Initial result: passed and pinned pre-run `Exit()` expectations.
- Production change: kept behavior, only nullable safety adjustments.
- Final result: passed.

### Event no-subscriber safety
- Test written first: `GameBase_PreRunLifecycleMethods_WithNoSubscribers_DoNotThrow`.
- Initial result: passed and documented no-subscriber event behavior.
- Production change: explicit nullable event declarations.
- Final result: passed.

## 6) Tests added
- `GameBase_Constructs_WithStablePreRunDefaults`
  - Locks pre-run host object availability and lifecycle state assumptions.
- `GameBase_PreRunLifecycleMethods_WithNoSubscribers_DoNotThrow`
  - Locks no-subscriber lifecycle event safety.
- `GameBase_Exit_BeforeRun_SetsIsExiting_AndDoesNotThrow`
  - Locks pre-run exit safety and state mutation.

## 7) Fixes applied
### `Host/GameBase.cs`
- Nullable event declarations for no-subscriber lifecycle events.
- Nullable internal lifecycle fields (`graphicsDeviceService`, `graphicsDeviceManager`, `resumeManager`) to match actual initialization timing.
- `Run(GameContext? gameContext = null)` nullable parameter contract tightened to match callsite usage.
- `UnhandledExceptionInternal` updated for nullable event flow.
- `resumeManager?.OnDestroyed()` in destroy cleanup path for pre-init safety.

Safety rationale: these changes preserve existing lifecycle behavior while reducing constructor-assumption nullability mismatches in host lifecycle phases.

## 8) Deferred warnings
Deferred on purpose (out of M12l scope):
- real backend/message loop integration details,
- graphics device/presenter details,
- SDL backend internals,
- non-GameBase warning buckets,
- remaining `GameBase` nullable flow warnings tied to deeper run/draw paths.

## 9) After warnings
After requested focused rebuild:
- Focused warning lines after: **84**
- Distribution after:
  - CS8618: 26
  - CS8625: 20
  - CS8602: 14
  - CS8600: 6
  - CS0162: 6
  - CS8604: 4
  - CS8603: 4
  - CS8601: 4
- `Host/GameBase` warning lines after: **16** (8 unique, duplicated by summary)
- Focused checker exit status: **4**
- Delta from M12k 110 baseline: **-26** focused warning lines.

## 10) Validation results
All required commands were executed.
- `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
  - exit: 0
  - first meaningful warning: `Host/GameBase.cs(99,95) CS8625`
  - pass/fail: pass (with warnings)
  - output truncated: no
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning: `PlatformWindowLifecycleTests.cs(56,54) CS8603` (test project warning)
  - pass/fail: pass
  - output truncated: no
- `./striv/build/striv-check-focused-project.sh Stride.Games`
  - exit: 4
  - first meaningful warning bucket: focused warnings remain
  - pass/fail: fail (expected gate behavior with remaining warnings)
  - output truncated: no
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
  - exit: 0
  - first meaningful warning: Stride.Games focused warning lines (same bucket)
  - pass/fail: pass (with warnings)
  - output truncated: yes (terminal capture truncated long output)
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit: 0
  - pass/fail: pass
  - output truncated: yes (combined command output)
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit: 0
  - pass/fail: pass
  - output truncated: yes (combined command output)
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0
  - pass/fail: pass
  - output truncated: yes (combined command output)
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0
  - pass/fail: pass
  - output truncated: yes (combined command output)
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - exit: 0
  - pass/fail: pass
  - output truncated: yes (combined command output)
- `./striv/build/striv-build-core.sh`
  - exit: 0
  - pass/fail: pass
  - output truncated: yes (combined command output)

## 11) Recommended next task
Another targeted lifecycle Shine pass focused on remaining `GameBase` draw/cleanup nullable flow sites (`CS8602`/`CS8625`) with tiny deterministic tests around initialization/cleanup boundaries, without entering real backend loop coverage yet.
