# CoreSmoke Runtime Run Feasibility Audit (M1h prep)

## 1) Evidence collection

### Commands used
- `find .. -name AGENTS.md -print`
- `rg -n "class Game|class GameBase|void Run|Run\(|Exit\(|IsExiting|BeginRun|EndRun|Update\(|Draw\(|GameContext|GameContextType|DesktopSDL|Headless|Null|Window|SDL|Vulkan|GraphicsDevice|GraphicsDeviceManager|ContentManager|EffectSystem|SceneSystem" sources/engine/Stride.Engine sources/engine/Stride.Games sources/engine/Stride.Graphics samples/StriV/CoreSmoke docs/stri-v build`
- `find samples/StriV/CoreSmoke -maxdepth 3 -type f | sort`
- `find sources/engine/Stride.Games -maxdepth 3 -type f | sort`
- `rg -n "CopyToOutputDirectory|Native|RuntimeIdentifier|RuntimeIdentifiers|OutputPath|AssemblyName|OutputType|TargetFramework|TargetFrameworks|SelfContained|UseAppHost" samples/StriV/CoreSmoke sources/sdk sources/targets build docs/stri-v/building-core.md`
- `nl -ba <file>` on each primary file listed below.

### Files opened/read
- `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`
- `samples/StriV/CoreSmoke/Program.cs`
- `build/striv-build-coresmoke-m1g.sh`
- `build/striv-build-coresmoke-m1g.ps1`
- `sources/engine/Stride.Engine/Engine/Game.cs`
- `sources/engine/Stride.Games/GameBase.cs`
- `sources/engine/Stride.Games/GameContextFactory.cs`
- `sources/engine/Stride.Games/GameContextSDL.cs`
- `sources/engine/Stride.Games/GameContextHeadless.cs`
- `sources/engine/Stride.Games/SDL/GameWindowSDL.cs`
- `sources/targets/Stride.targets`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Graphics.targets`
- `docs/stri-v/building-core.md`

### Scripts used
- No custom scripts created or executed.

### Modification confirmation
- Static analysis only for repo content; no builds/tests/executables were run.
- No existing source files were edited as part of this audit (only this report file was created).

### Static-only uncertainty
- Runtime feasibility in this sandbox cannot be proven without executing `dotnet` + native graphics/SDL stack.
- Native dependency presence (SDL2/Vulkan ICD/display server) is inferred from code/targets, not validated by run.

---

## 2) Current M1g baseline recap

- CoreSmoke is a minimal code-first executable (`OutputType=Exe`, `TargetFramework=net10.0`) with direct `ProjectReference` to `Stride.Engine`, no asset/editor project structure.  
- Program body is currently only `new Game(); game.Run();` (no custom subclass, no explicit context, no exit policy).  
- M1g build scripts enforce Linux + Vulkan + opt-outs (`StrideIncludeShaderCompiler=false`, `StrideIncludeAudio=false`, `StrideIncludeVirtualReality=false`) and source-built AssemblyProcessor routing.
- M1g proves compile/build graph viability for this executable slice.
- M1g does **not** prove startup/run-loop, window/device creation, or clean exit behavior.

---

## 3) CoreSmoke executable output audit

### Expected output path
Because CoreSmoke uses plain `Microsoft.NET.Sdk` and does not set custom `OutputPath`, expected output is standard SDK layout:
- Debug: `samples/StriV/CoreSmoke/bin/Debug/net10.0/`
- Release: `samples/StriV/CoreSmoke/bin/Release/net10.0/`

Expected primary managed entrypoint:
- `samples/StriV/CoreSmoke/bin/<Config>/net10.0/StriV.CoreSmoke.dll`

Likely additional host files (default .NET apphost behavior if not disabled):
- `StriV.CoreSmoke` (native launcher on Linux), plus `.deps.json` and `.runtimeconfig.json`.

### Run command
Safest canonical command:
- `dotnet samples/StriV/CoreSmoke/bin/<Config>/net10.0/StriV.CoreSmoke.dll`

Possible secondary command (if apphost emitted/executable bit set):
- `./samples/StriV/CoreSmoke/bin/<Config>/net10.0/StriV.CoreSmoke`

### Output variability
- For CoreSmoke itself, path should not include graphics API segment because it is not using Stride Sdk output path override.
- Referenced Stride projects (using Stride SDK) are API-dimensioned internally (`.../<TFM>/<StrideGraphicsApi>/`), but that does not necessarily alter CoreSmoke top-level output folder.
- M1g scripts do not currently print explicit CoreSmoke output location.

### Native library copy expectation
- Stride SDK includes `.ssdeps`-based native dependency copy system for Stride projects; CoreSmoke relies transitively on referenced runtime projects for native payload propagation.
- Static audit cannot guarantee all required native runtime files land in CoreSmoke output until a run/publish probe is executed.

---

## 4) Program/run behavior audit

### Current behavior
- `Program.cs` directly creates `Game` and calls `Run()`. No context argument, no frame limit, no timeout, no exit call.

### Exit semantics
- `GameBase.Run()` enters platform loop.
- `Exit()` exists on `GameBase` and sets `IsExiting=true` + platform exit request.
- Without explicit `Exit()`, process is expected to continue until window close or external termination.

### Minimal code change for deterministic smoke exit
Recommended minimal change:
- Introduce `CoreSmokeGame : Game` subclass in `Program.cs` (or separate file).
- Override `Update(GameTime)`; call `Exit()` after first update/frame (or short frame counter/time threshold).

Why this is minimal/clean:
- Uses built-in lifecycle (`Update` + `Exit`) without touching renderer/content pipelines.
- Avoids external kill/timeout as primary pass condition.

---

## 5) Game startup/context audit

- If `Run(null)` is used on Linux, `GameBase.Run()` selects `AppContextType.DesktopSDL` by default.
- `GameContextFactory` maps that to `GameContextSDL` (if `STRIDE_UI_SDL` is active).
- `GameContextSDL` creates a `GameFormSDL` by default.
- SDL window loop (`SDLMessageLoop.Run`) is used for blocking run; it will continue until exit path triggers.
- `InitializeBeforeRun()` calls `graphicsDeviceManager.CreateDevice()` and requires a valid `IGraphicsDeviceService`/`GraphicsDevice`.
- `Game` constructor registers `GraphicsDeviceManager` service by default.

### Headless/null context
- `GameContextHeadless` exists and `AppContextType.Headless` is defined in factory.
- Static evidence does **not** yet prove full headless path safety for this exact M1g route (especially with full `Game` + graphics initialization still happening).
- Therefore, headless should be treated as investigational, not assumed production-ready for M1h smoke.

### Assets/effects/shader compiler considerations
- `PrepareContext()` creates `ContentManager` service; this does not by itself require project assets.
- No intentional asset loads in current Program.
- With shader compiler excluded, runtime may still work for trivial startup if no dynamic effect compilation is requested; static audit cannot guarantee all draw/init paths avoid compiler-dependent behavior.

---

## 6) Native/runtime environment audit

Likely runtime dependencies for first launch:
- SDL2 native stack (window/events).
- Vulkan loader + valid ICD.
- Display server access (X11/Wayland) for DesktopSDL path.
- Other transitive native libs from Stride runtime graph (e.g., image/font related).

Helpful environment knobs for diagnostics:
- `SDL_VIDEODRIVER` (e.g., x11/wayland/offscreen where supported)
- `VK_ICD_FILENAMES` (force explicit Vulkan ICD JSON)

Sandbox viability assessment:
- Running a graphical SDL/Vulkan loop in CI/container sandbox is high-risk and likely environment-constrained.
- Recommendation: treat runtime smoke as **opt-in**, best-effort in sandbox, authoritative on developer machine with known graphics stack.

Blocker classification guidance:
- **Environment limitation**: missing display, SDL init failure, missing Vulkan loader/ICD, container GPU access issues.
- **Engine/runtime bug**: reproducible failure on properly configured local Linux dev machine with display/Vulkan present and minimal self-exit logic.

---

## 7) Candidate M1h implementation options

### Option A — compile-only + no-op `--run` placeholder
- Changes: script placeholder only.
- Blockers: does not test runtime.
- Proves: nothing beyond M1g.
- Recommendation: reject.

### Option B — run as-is
- Changes: none.
- Blockers: likely indefinite loop/hang.
- Proves: startup only if manually interrupted.
- Recommendation: weak.

### Option C — self-exit `CoreSmokeGame : Game`
- Changes: minimal code update in CoreSmoke program + run script.
- Blockers: true runtime blockers surfaced quickly.
- Proves: launch + at least one update tick + clean shutdown path.
- Recommendation: **primary**.

### Option D — headless/no-window
- Changes: explicit headless context use and maybe additional guards.
- Blockers: uncertain support maturity for this route.
- Proves: non-window startup only.
- Recommendation: only as secondary experiment.

### Option E — external timeout kill
- Changes: run wrapper with timeout.
- Blockers: can mask clean-exit behavior.
- Proves: process started and survived until timeout (not graceful exit).
- Recommendation: use as safety fallback only.

---

## 8) Proposed M1h implementation target

Smallest credible target:
1. Add `CoreSmokeGame : Game` that exits after first `Update`.
2. Keep rendering/content untouched.
3. Add dedicated opt-in run wrappers:
   - `build/striv-run-coresmoke-m1h.sh`
   - `build/striv-run-coresmoke-m1h.ps1`
4. Run wrappers call existing M1g build first, then execute CoreSmoke dll.
5. Wrap run in bounded timeout for safety (fallback guard).

Command pattern (conceptual):
- Build: `./build/striv-build-coresmoke-m1g.sh <Debug|Release>`
- Run: `dotnet samples/StriV/CoreSmoke/bin/<Config>/net10.0/StriV.CoreSmoke.dll`
- Safety: timeout wrapper + capture first failure signature.

Blocker classification strategy in script output:
- Parse/label common SDL/Vulkan/display failures as environment limitation.
- Otherwise label as potential engine/runtime blocker pending local reproduction.

---

## 9) Risk register

| Risk | Area | Likelihood | Impact | Evidence | Mitigation |
| ---- | ---- | ---------: | -----: | -------- | ---------- |
| CoreSmoke hangs forever | Run loop | High | Medium | Current program calls `Game.Run()` with no exit trigger | Add self-exit in `Update()` |
| SDL display unavailable | Environment | High (sandbox) | High | Linux default context is DesktopSDL/windowed | Opt-in run; classify as environment |
| Vulkan loader/ICD unavailable | Environment | Medium-High | High | Vulkan route is explicitly selected in M1g | Emit diagnostic + env hints |
| Graphics device creation fails | Runtime init | Medium | High | `CreateDevice()` is mandatory during init | Keep smoke tiny; report first exception |
| Missing native libs in output | Packaging/runtime | Medium | High | Native copy is transitive/.ssdeps-based | Verify by run; include missing-lib signature |
| Runtime attempts content/effect load unexpectedly | Engine subsystems | Low-Med | Medium | `ContentManager` created by default | Avoid scenes/assets/effect requests |
| No-shader-compiler mode breaks early effect path | Engine config | Low-Med | Medium | Shader compiler intentionally disabled | Keep smoke pre-render/minimal |
| No-audio/no-VR assumptions break startup | Engine config | Low | Medium | Features are compile-time gated in Game | Keep defaults; report first failure |
| Sandbox cannot run graphical app | Infra | High | High | Typical container lacks display/GPU integration | Mark as non-authoritative sandbox blocker |
| Works locally but fails in sandbox | Parity | High | Medium | Display/GPU env differs | Treat local dev machine as authority |

---

## 10) Recommended implementation prompt (for next Codex task)

Implement M1h runtime smoke with minimal changes:

1. Update `samples/StriV/CoreSmoke/Program.cs` to run a `CoreSmokeGame : Game` subclass that calls `Exit()` after first `Update(GameTime)` (or very short frame counter), with no asset/editor/shader-compiler/audio/VR additions.
2. Add opt-in run scripts:
   - `build/striv-run-coresmoke-m1h.sh`
   - `build/striv-run-coresmoke-m1h.ps1`
3. Scripts must invoke existing M1g build scripts first (Debug default, Release optional), then run CoreSmoke using `dotnet <dll>` from `samples/StriV/CoreSmoke/bin/<Configuration>/net10.0/`.
4. Add timeout safety so run never hangs indefinitely.
5. On failure, print concise classification:
   - environment limitation (SDL/display/Vulkan/loader/ICD/native-lib)
   - potential engine/runtime blocker (if not environment-signature).
6. Do not fix runtime blockers beyond minimal smoke plumbing; stop at first blocker and report exact command + error.
