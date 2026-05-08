# 1320 M12a — Stride.Games Project Boundary Sort Plan (Planning-Only)

## 1) Files changed
- `docs/stri-v/audits/1000+/1320-m12a-stride-games-project-boundary-sort-plan.md` (new report only).

## 2) Task scope
- This is a **planning-only 5S Sort audit** for `Stride.Games`.
- No file moves, no file deletions, no warning fixes, and no behavior refactors were performed.
- Goal: define the **future responsibility boundary** before M12b physical Sort edits.

## 3) Current project inventory

### Inventory counts
- File inventory command produced **53** files under `striv/projects/Stride.Games`.
- Compile item snapshot (`msbuild -getItem:Compile`) produced **874** compile entries (indicates wildcard include + linked/generated/transitive compile item expansion in evaluated project graph).

### csproj structure and dependencies
- `Stride.Games.csproj` uses `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>` and explicitly includes `**/*.cs` (excluding `bin/obj`).
- It links `../../../sources/shared/SharedAssemblyInfo.cs`.
- It explicitly removes `Starter/StrideActivity.cs` from compile (Android launcher surface exclusion).
- It directly references `Stride.Graphics`.

### Major file groups observed in `striv/projects/Stride.Games`
- **Host context / platform context types**:
  - `GameContext.cs`, `GameContextFactory.cs`, `GameContextHeadless.cs`, `GameContextSDL.cs`, `GameContextWinforms.cs`, `GameContextAndroid.cs`, `GameContextiOS.cs`, `GameContextUWP.cs`.
- **Game loop and lifecycle core**:
  - `GameBase.cs`, `GamePlatform.cs`, `GameTime.cs`, `GameUnhandledExceptionEventArgs.cs`, `LaunchParameters.cs`.
- **System orchestration abstractions**:
  - `IGameSystemBase.cs`, `IGameSystemCollection.cs`, `GameSystemBase.cs`, `GameSystemCollection.cs`, `GameSystemState.cs`, `IUpdateable.cs`, `IDrawable.cs`.
- **Windowing and message loop**:
  - `GameWindow.cs`, `GameWindowHeadless.cs`, `IMessageLoop.cs`, `Desktop/*`, `SDL/*`.
- **Graphics/presenter hooks**:
  - `GraphicsDeviceManager.cs`, `GraphicsDeviceInformation.cs`, `IGraphicsDeviceFactory.cs`, `IGraphicsDeviceManager.cs`, `GameGraphicsParameters.cs`, `PreparingDeviceSettingsEventArgs.cs`, `GameWindowRenderer.cs`.
- **Misc / docs / utility**:
  - `AssemblyDoc.cs`, `NamespaceDoc.cs`, `ListBoundExtensions.cs`, `Time/*`, properties/docs.
- **Non-runtime/platform residuals**:
  - `Platforms/Android/Resources/Layout/stride_popup_edittext.xml`, `Starter/StrideActivity.cs` (excluded from compile).

## 4) Current dependency map

### Who references `Stride.Games`
ProjectReference hits:
- `striv/projects/Stride.Input/Stride.Input.csproj`
- `striv/projects/Stride.Rendering/Stride.Rendering.csproj`
- `striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj`

### Frequently referenced symbols (from cross-project/text evidence)
- `GameTime` and `GameContext*` are heavily consumed by `Stride.Input` and input tests.
- `GameContextHeadless` is used in runtime smoke/lifecycle tests.
- `GameContextWinforms` / `GameContextSDL` / mobile/UWP contexts are referenced by `Stride.Input` input-source selection paths.

### Dependency interpretation
- `Stride.Input` is currently coupled to `Stride.Games` for host/context abstraction and timing contract.
- `Stride.Rendering` references `Stride.Games`, indicating game-loop/system coupling beyond raw graphics.
- Any boundary shrink in `Stride.Games` must preserve an API compatibility path for `GameContext` + `GameTime` and likely platform-window handles until dependent projects are adjusted.

## 5) Proposed one-sentence project purpose
**Recommended purpose statement:**

`Stride.Games owns the minimal desktop runtime host contract: game loop lifecycle, timing, host context abstraction, and host-facing system orchestration used by higher-level runtime projects.`

## 6) Responsibility classification table

| Area/file group | Current role | Classification | Proposed home/action | Rationale |
| --- | --- | --- | --- | --- |
| `GameBase`, `IGame`, lifecycle/tick/run flow | Top-level game host loop and lifecycle | Keep in `Stride.Games` | Keep | Core of minimal host/run-loop mission |
| `GameTime`, `Time/*` | Timing and tick primitives | Keep in `Stride.Games` | Keep | Shared loop contract used by input/runtime tests |
| `GameContext` base + `GameContextHeadless` | Host context abstraction + non-window runtime context | Keep in `Stride.Games` | Keep | Essential contract for desktop/core test hosting |
| `GameSystem*`, `IUpdateable`, `IDrawable` | In-loop system scheduling and draw/update ordering | Defer | Keep for now; evaluate later split to `Stride.RuntimeHost` or Engine-facing layer | Could be host-level orchestration or better owned above in Engine/policy layer |
| `GameWindow`, `IMessageLoop`, abstract window glue | Generic window/message-loop abstraction | Split later | `Stride.Windowing` or `Stride.Games.Desktop` | Host boundary wants abstraction; concrete window stacks should separate |
| `Desktop/*` WinForms/Win32 message pump | Windows-specific platform backend | Split later | `Stride.Games.Desktop.Win32` or `Stride.Windowing.Win32` | Platform backend is implementation, not core contract |
| `SDL/*` SDL window/form/message loop | SDL-specific backend | Split later | `Stride.Games.SDL` / `Stride.Windowing.SDL` | Backend-specific code should be modularized |
| `GraphicsDeviceManager*`, `GameWindowRenderer`, `GraphicsDeviceInformation`, `GameGraphicsParameters`, graphics interfaces | Device/presenter lifecycle tightly tied to graphics ownership | Split later | Primarily `Stride.Graphics` (or `Stride.Presentation`) with thin host callback contracts in `Stride.Games` | Violates single-purpose host goal if graphics ownership remains here |
| `IContentable` and `Content` lifecycle touchpoints in game host | Content service exposure | Defer | Possibly stay as interface contract, with implementation owned by Engine/asset runtime | Needs proof whether required for minimal host API compat |
| `GameContextAndroid`, `GameContextiOS`, `GameContextUWP*`, Android resources, `Starter/StrideActivity.cs` | Mobile/UWP legacy contexts and assets | Remove/quarantine | Quarantine in legacy/mobile module (not Stri-V core) | Out of desktop-first scope; already partly excluded in csproj |
| Editor/GameStudio/Quantum concerns | Not observed directly in this project tree | Keep out | N/A | Should remain excluded from runtime core by policy |

## 7) Proposed future project/module splits (planning only)
- `Stride.Games` (kept minimal):
  - loop lifecycle, `GameTime`, base `GameContext`, headless context, host contracts.
- `Stride.Games.Desktop`:
  - desktop-specific host bootstrap and integration facade.
- `Stride.Windowing` (+ platform slices):
  - platform-neutral window contracts; `Stride.Windowing.Win32`, `Stride.Windowing.SDL` implementations.
- `Stride.Presentation` (optional) or `Stride.Graphics` expansion:
  - presenter/window renderer coupling and swapchain/presentation lifecycle.
- `Stride.Games.LegacyMobile` (quarantine) or archive path:
  - Android/iOS/UWP contexts and resources.
- Future `InputMan` (not now):
  - action mapping/policy/rebinding; keep out of both `Stride.Input` and `Stride.Games`.
- Future Dominatus integration layer:
  - advanced policy/state-machine lifecycle orchestration above host loop.

## 8) Sort candidates for M12b

### High-confidence quarantine/remove candidates
- `GameContextAndroid.cs`
- `GameContextiOS.cs`
- `GameContextUWP.cs`
- `Platforms/Android/**`
- `Starter/StrideActivity.cs` (already compile-removed; candidate for physical quarantine)

### High-confidence organization moves (without behavior change)
- Group host contracts into `Host/` (e.g., `GameBase`, `GameContext`, `GameTime`, interfaces).
- Group platform implementations under `Backends/Desktop.Win32/` and `Backends/SDL/`.
- Group graphics-adjacent files under `GraphicsBridge/` pending later split.

### Leave untouched in M12b
- API-facing types heavily used by `Stride.Input` and tests (`GameTime`, `GameContext`, `GameContextHeadless`) unless adapter stubs are provided.
- Core run-loop/lifecycle entry behavior in `GameBase` and `GamePlatform`.

### First build risks in M12b
- `Stride.Input` compile breaks due to direct type checks on `GameContext*` subclasses.
- `Stride.Rendering` reliance on `Stride.Games` contracts.
- Hidden runtime assumptions for window handles/presenter ownership.

## 9) Focused warning baseline
- Focused build command generated **292 warning lines** attributable to `Stride.Games` paths.
- Top warning codes:
  - `CS8618`: 122
  - `CS8622`: 66
  - `CS8625`: 46
  - `CS8603`: 22
  - `CS8601`: 10
  - `CS8600`: 8
  - `CS0162`: 6
  - `CS8604`: 4
  - `CS8602`: 4
  - `CS8767`: 2
  - `CS8073`: 2
- Likely warning clusters:
  - Nullability/event initialization in lifecycle/window/system classes.
  - Delegate nullability mismatches across event handlers.
  - Nullability returns in factory/context/platform code.
  - Small unreachable branches in graphics/device manager.

## 10) Risks
- **Engine/GameSystem lifecycle coupling risk**: `GameSystemCollection` and `GameBase` assumptions may implicitly define runtime order semantics consumed by Engine/Rendering.
- **Platform host assumption risk**: Input and window-dependent paths use concrete `GameContext*` type checks.
- **Window/presenter ownership confusion**: `GameWindowRenderer` + `GraphicsDeviceManager` blur host-vs-graphics boundaries.
- **Input lifecycle interaction risk**: `Stride.Input` currently derives source selection from `GameContext` concrete types.
- **Graphics device coupling risk**: device creation/reset and presentation callbacks currently sit in `Stride.Games`.
- **Future Dominatus integration risk**: premature extraction of orchestration types could conflict with planned policy/lifecycle state-machine ownership.

## 11) Recommended M12b prompt

**Proposed next prompt (copy/paste):**

> Perform M12b Sort implementation for `Stride.Games` using the M12a boundary plan. Move/quarantine only high-confidence out-of-scope items (mobile/UWP/Android/iOS assets and contexts) and apply folder organization only (no behavior refactor). Preserve public API compatibility where currently referenced by `Stride.Input` and `Stride.Rendering`, or add temporary adapters if needed. Do not fix warnings except those required to keep build passing after moves. Build `Stride.Games`, then build the full core script, and report file moves, compile deltas, and warning deltas.

## 12) Validation

### Command results
1. `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
   - Exit code: `0`
   - First meaningful warning/error: `warning CS8767` in `GamePlatform.ChangeOrCreateDevice` nullability signature mismatch.
   - Pass/fail: **Pass with warnings**.
   - Output truncated: **Yes** (interactive capture truncated very long stream in this environment).

2. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `1`
   - First meaningful warning/error: `MSB4018 GenerateDepsFile` IO lock on `Stride.Core.deps.json`.
   - Pass/fail: **Fail**.
   - Output truncated: **No** (failure context captured).

3. `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: first warnings were existing nullability/perf warnings in core projects.
   - Pass/fail: **Pass with warnings**.
   - Output truncated: **Yes** (very large output stream).
