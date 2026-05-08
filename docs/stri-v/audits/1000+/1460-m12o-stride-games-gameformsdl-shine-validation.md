# 1460 — M12o Stride.Games GameFormSDL Shine validation

## 1) Files changed

- `striv/projects/Stride.Games/SDL/GameFormSDL.cs`
- `docs/stri-v/audits/1000+/1460-m12o-stride-games-gameformsdl-shine-validation.md`

## 2) Task scope

M12o targeted `SDL/GameFormSDL.cs` only for the event/nullability bucket (CS8618).

No SDL backend refactor was performed:
- no SDL initialization changes,
- no message loop changes,
- no platform selection changes,
- no event order changes.

No native SDL runtime/window test was added because this pass was a pure no-subscriber event nullability annotation cleanup and existing lifecycle tests already validate the no-subscriber pattern through non-SDL probes.

## 3) Before warnings

Build command:

```bash
dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Games \
  --no-incremental
```

Focused warning count before: **68** (`/tmp/striv-m12o-games-warning-lines-before.log`).

Warning distribution before:
- CS8618: 26
- CS8625: 12
- CS8602: 6
- CS8600: 6
- CS0162: 6
- CS8604: 4
- CS8603: 4
- CS8601: 4

`GameFormSDL` warning count before: **12 logged lines** (duplicated build summary), representing **6 unique CS8618 event sites**:
- `AppActivated`
- `AppDeactivated`
- `PauseRendering`
- `ResumeRendering`
- `SizeChanged`
- `FullscreenToggle`

Evidence file: `/tmp/striv-m12o-gameformsdl-warnings-before.log`.

## 4) SDL/GameFormSDL lifecycle map

- `GameFormSDL` subscribes to SDL/window action hooks in constructor (`SizeChangedActions`, activate/deactivate, minimized/maximized/restored, keyboard).
- Events are standard .NET publication points for window lifecycle and are raised through null-conditional invocation (`?.Invoke`).
- Event delegates are intentionally allowed to have no subscribers during construction/lifecycle transitions.
- `previousWindowState` is constructor-initialized (`FormWindowState.Normal`) and used to gate pause/resume behavior.

Ownership/lifecycle:
- SDL/window callbacks are runtime-owned and connected by constructor wiring.
- Event subscribers are external consumer-owned and may be absent.

Test decision:
- No new SDL-native tests added: the warnings were strictly nullable event declaration mismatches with already-safe `?.Invoke` usage; validating this does not require native SDL window creation and would add flaky environment coupling.

## 5) Tests

No new tests were added.

Rationale: this pass was pure event nullability annotation cleanup. Existing `Stride.Games.Tests` lifecycle tests already validate no-subscriber event semantics in platform/window probes without requiring native SDL runtime.

## 6) Fixes applied

### `striv/projects/Stride.Games/SDL/GameFormSDL.cs`

Warnings addressed:
- CS8618 on six event declarations.

Changes:
- Marked events nullable:
  - `AppActivated` -> `EventHandler<EventArgs>?`
  - `AppDeactivated` -> `EventHandler<EventArgs>?`
  - `PauseRendering` -> `EventHandler<EventArgs>?`
  - `ResumeRendering` -> `EventHandler<EventArgs>?`
  - `SizeChanged` -> `EventHandler<EventArgs>?`
  - `FullscreenToggle` -> `EventHandler<EventArgs>?`

Behavior safety rationale:
- Events were already invoked via `?.Invoke`, so nullable declaration matches runtime behavior and no-subscriber semantics.
- No lifecycle logic, callback ordering, or SDL runtime behavior was modified.

## 7) After warnings

Focused warning count after: **56** (`/tmp/striv-m12o-games-warning-lines-after.log`).

Warning distribution after:
- CS8618: 14
- CS8625: 12
- CS8602: 6
- CS8600: 6
- CS0162: 6
- CS8604: 4
- CS8603: 4
- CS8601: 4

`GameFormSDL` warning count after: **0** (`/tmp/striv-m12o-gameformsdl-warnings-after.log`).

Focused checker:
- `./striv/build/striv-check-focused-project.sh Stride.Games`
- exit status: **4** (gate still failing due to remaining focused warnings)

Delta from M12n baseline (68):
- **68 -> 56** (improvement of **12 logged warning lines**, corresponding to removal of the `GameFormSDL` event bucket).

## 8) Remaining warning bucket analysis

Command:

```bash
sed -E 's#.*striv/projects/Stride.Games/([^(:]+).*warning ((CS|CA|NU|STRIDE)[0-9]+).*#\1 \2#' \
  /tmp/striv-m12o-games-warning-lines-after.log \
  | sort | uniq -c | sort -nr | head -n 40
```

Top buckets:
- `Host/GameContext.cs CS8618` — 6
- `GraphicsBridge/GraphicsDeviceManager.cs CS0162` — 6
- `Systems/GameSystemCollection.cs CS8604` — 4
- `Systems/GameSystemCollection.cs CS8600` — 4
- `SDL/GameWindowSDL.cs CS8618` — 4
- `Host/GameContextFactory.cs CS8625` — 4
- `GraphicsBridge/GraphicsDeviceManager.cs CS8625` — 4

Recommended next highest-return bucket:
- **`Host/GameContext.cs` CS8618**
  - warning count: highest contiguous actionable nullability bucket (6)
  - confidence: high (constructor/property nullability contract cleanup)
  - testability: high (non-SDL, easily covered by unit tests)
  - risk: low-to-moderate (host context contracts, no native runtime coupling)

## 9) Validation results

1. Command:
   `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
   - Exit code: 0
   - First meaningful warning/error: `GamePlatformDesktop.cs(72,24): warning CS8603`
   - Pass/fail: Pass (build)
   - Output truncated: No

2. Command:
   `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none; tests passed (23)
   - Pass/fail: Pass
   - Output truncated: No

3. Command:
   `./striv/build/striv-check-focused-project.sh Stride.Games`
   - Exit code: 4
   - First meaningful warning/error: focused warning gate failed (56 warnings remain)
   - Pass/fail: Fail (expected until focused warnings are reduced to gate threshold)
   - Output truncated: No

4. Command:
   `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
   - Exit code: 0
   - First meaningful warning/error: `GamePlatformDesktop.cs(72,24): warning CS8603`
   - Pass/fail: Pass
   - Output truncated: Yes (console excerpt shortened in run log)

5. Command:
   `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: Pass
   - Output truncated: No

6. Command:
   `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: Pass
   - Output truncated: No

7. Command:
   `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: Pass
   - Output truncated: No

8. Command:
   `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: Pass
   - Output truncated: No

9. Command:
   `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none (1 test skipped by suite metadata)
   - Pass/fail: Pass
   - Output truncated: No

10. Command:
    `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
    - Exit code: 0
    - First meaningful warning/error: none
    - Pass/fail: Pass
    - Output truncated: No

11. Command:
    `./striv/build/striv-build-core.sh`
    - Exit code: 0
    - First meaningful warning/error: none
    - Pass/fail: Pass
    - Output truncated: Yes (long AP/build trace abbreviated in terminal capture)

## 10) Recommended next task

Next task: target `Host/GameContext.cs` CS8618 constructor/property contract bucket.

Reason:
- highest-return remaining focused nullability bucket,
- isolated file-level work,
- high-confidence testability without SDL/native runtime,
- low risk versus SDL/window lifecycle internals.
