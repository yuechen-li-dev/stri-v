# 1380 — M12g Stride.Games tested-lifecycle Shine validation

## 1) Files changed

- `striv/projects/Stride.Games/Host/GameContextHeadless.cs`
- `striv/projects/Stride.Games/Windowing/GameWindowHeadless.cs`
- `striv/projects/Stride.Games/Systems/GameSystemBase.cs`
- `striv/projects/Stride.Games/Systems/GameSystemCollection.cs`
- `docs/stri-v/audits/1000+/1380-m12g-stride-games-tested-lifecycle-shine-validation.md`

## 2) Task scope

M12g targeted lifecycle nullability warnings only in tested/headless areas covered by M12f tests:

- `Host/GameContextHeadless`
- `Windowing/GameWindowHeadless`
- `Systems/GameSystemCollection`
- `Systems/GameSystemBase`

Heavier lifecycle buckets were intentionally deferred and not refactored:

- GraphicsBridge lifecycle (`GraphicsDeviceManager`, `GameWindowRenderer`)
- Full `GameBase` run-loop lifecycle
- Real platform window lifecycle (`GameWindow`, SDL/Desktop loops)

## 3) Before warnings

From `/tmp/striv-m12g-games-before.log` parsing:

- Focused warning count before: **204**
- Distribution before:
  - `CS8618`: 120
  - `CS8625`: 44
  - `CS8603`: 8
  - `CS8601`: 8
  - `CS8604`: 6
  - `CS8602`: 6
  - `CS0162`: 6
  - `CS8600`: 4
  - `CS8073`: 2
- Tested-area warning lines before grep subset: **24** lines (includes duplicate lines from command output replay)
  - Unique sites included:
    - `Windowing/GameWindowHeadless.cs(30,50)` `CS8603`
    - `Host/GameContextHeadless.cs(18,16)` `CS8625`
    - `Systems/GameSystemBase.cs(63,19)` `CS8618` (events)
    - `Systems/GameSystemCollection.cs(261,30)` `CS8600`
    - `Systems/GameSystemCollection.cs(266,17)` `CS8602`
    - `Systems/GameSystemCollection.cs(271,40)` `CS8604`
    - `Systems/GameSystemCollection.cs(309,30)` `CS8600`
    - `Systems/GameSystemCollection.cs(313,43)` `CS8604`
    - `Systems/GameSystemCollection.cs(389,83)` `CS8625`

## 4) Tests used / added

- Ran M12f lifecycle tests before fixes:
  - `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (pass)
- Re-ran same tests after fixes (pass).
- No new tests added.

Why these protect changes:

- Existing tests exercise deterministic headless construction/lifecycle (`GameContextHeadless`, `GameWindowHeadless`) and system collection ordering/filtering add-remove/update-draw flow, which directly covers edited code paths.

## 5) Fixes applied

### `Host/GameContextHeadless.cs`

- Warning addressed: constructor passed `null` into non-nullable `GameContext<object>` control.
- Change: switched inheritance to `GameContext<object?>` so headless control is explicitly nullable.
- Behavior impact: none; headless still passes `null` control by design.

### `Windowing/GameWindowHeadless.cs`

- Warning addressed: possible null return from `NativeWindow` override.
- Change: kept headless semantics (no native window) and returned `null!` with explicit lifecycle rationale comment.
- Behavior impact: none; still represents absence of OS window in headless mode.

### `Systems/GameSystemBase.cs`

- Warning addressed: non-nullable events with no constructor assignment (`CS8618`).
- Change: made change-notification events nullable (`EventHandler<EventArgs>?`) while preserving invocation via `?.Invoke`.
- Behavior impact: none; event semantics unchanged and already allow no subscribers.

### `Systems/GameSystemCollection.cs`

- Warning addressed: nullable flow around temporary `KeyValuePair<T, ProfilingKey>` initialized with `null` profile key.
- Change: replaced null-seeded key construction with nullable local key container and non-null profiling key creation on insertion paths.
- Behavior impact: none to ordering/filtering; insertion, ordering, and remove/update behavior unchanged.

## 6) After warnings

From `/tmp/striv-m12g-games-after.log` parsing:

- Focused warning count after: **190**
- Distribution after:
  - `CS8618`: 112
  - `CS8625`: 40
  - `CS8601`: 8
  - `CS8604`: 6
  - `CS8603`: 6
  - `CS8602`: 6
  - `CS0162`: 6
  - `CS8600`: 4
  - `CS8073`: 2
- Tested-area warning lines after grep subset: **10** lines (duplicate replay included)
  - Remaining unique tested subset warnings are only in `Systems/GameSystemCollection.cs` at add/remove loops (`CS8600/CS8602/CS8604`).
- Focused checker status:
  - `./striv/build/striv-check-focused-project.sh Stride.Games` => exit **4** (expected while warnings remain)
- Delta vs M12e/M12f baseline 204:
  - **-14 warnings** (204 -> 190)

## 7) Deferred warnings

Still deferred by design:

- GraphicsBridge lifecycle/null-flow (`GraphicsDeviceManager`, `GameWindowRenderer`, `GameGraphicsParameters`)
- `GameBase` run-loop lifecycle warnings
- Real `GameWindow` and platform window lifecycle
- SDL message loop lifecycle/null-flow
- Other hard null-flow buckets requiring deeper lifecycle refactors

## 8) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` | 0 | `Host/GameBase.cs(419,51) CS8625` | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (pre-fix) | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (post-fix) | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` (post-fix) | 0 | `Host/GameBase.cs(419,51) CS8625` | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Games` | 4 | `Focused warning gate failed for Stride.Games` | Expected fail | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` | 0 | `Host/GameBase.cs(419,51) CS8625` | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | One known skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | No |

No MSB3026 retry noise observed in this run set.

## 9) Recommended next task

**Recommend: M12h targeted tests for GraphicsBridge/window lifecycle.**

Rationale:

- Remaining warning concentration is now mostly outside M12g tested/headless slice and clustered in GraphicsBridge + broader host/window lifecycle.
- Additional targeted tests would provide safety rails for the next warning-reduction pass without risking run-loop/window/presenter regressions.
