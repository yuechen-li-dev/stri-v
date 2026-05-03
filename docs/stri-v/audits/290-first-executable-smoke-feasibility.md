# Stri-V M1g-prep: First executable/runtime smoke feasibility (static audit)

## 1) Evidence collection

### Commands executed
- `find /workspace -name AGENTS.md -o -name SKILL.md | head -n 50`
- `rg -n "static void Main|class Program|new Game\(|Game\.Run|Run\(|GameContext|GameContextSDL|GameContextWinforms|GraphicsDeviceManager|SceneSystem|Entity\(|ScriptComponent|GraphicsDevice|Window|SwapChain|Clear|BepuSimulation|PhysicsGameSystem" samples sources/engine sources/core build docs/stri-v`
- `find samples -maxdepth 4 -type f \( -name '*.csproj' -o -name '*.cs' -o -name '*.sln' -o -name '*.slnf' \) | sort`
- `rg -n "<ProjectReference|<PackageReference|OutputType|TargetFramework|TargetFrameworks|StrideRuntime|StrideAssemblyProcessor|StrideGraphicsApiDependent|StrideCompileAssets|StridePackage|StridePlatforms|StrideGraphicsApis|UseWPF|UseWindowsForms|RuntimeIdentifier|RuntimeIdentifiers" samples sources/engine build`
- `sed` inspections of:
  - `sources/engine/Stride.Engine/Engine/Game.cs`
  - `sources/engine/Stride.Games/GameBase.cs`
  - `sources/engine/Stride.Games/GameContextSDL.cs`
  - `sources/engine/Stride.Games/Stride.Games.csproj`
  - `sources/engine/Stride.Engine/Stride.Engine.csproj`
  - `samples/Physics/BepuSample/BepuSample.Windows/BepuSampleApp.cs`
  - `samples/Physics/BepuSample/BepuSample.Windows/BepuSample.Windows.csproj`
  - `samples/Physics/BepuSample/BepuSample.Game/BepuSample.Game.csproj`
  - `samples/Games/JumpyJet/JumpyJet.Windows/JumpyJetApp.cs`
  - `samples/Games/JumpyJet/JumpyJet.Windows/JumpyJet.Windows.csproj`
  - `build/StriV.Engine.Bepu.M1f.slnf`
  - `build/striv-build-engine-bepu-m1f.sh`

### Search terms used
- Entry/runtime: `static void Main`, `new Game()`, `Game.Run`, `GameContext`, `GameContextSDL`, `GraphicsDeviceManager`.
- Runtime systems: `SceneSystem`, `Entity`, `ScriptComponent`, `PhysicsGameSystem`, `BepuSimulation`.
- Build metadata: `OutputType`, `TargetFramework`, `StrideRuntime`, `StrideCompileAssets`, `StrideGraphicsApis`, opt-out properties.

### Files reviewed
- Engine runtime/startup internals under `sources/engine/Stride.Games` and `sources/engine/Stride.Engine`.
- Representative sample executables and sample game projects under `samples/`.
- Existing M1f solution filter/script under `build/`.

### Scripts used
- No custom scripts created.
- Only repository scripts/files were read.

### File modification confirmation
- This audit is static analysis only; no build/test/run commands were executed.
- No source/project files were changed as part of analysis logic.

### Static-only uncertainty
- Headless viability and Vulkan/SDL runtime behavior cannot be proven without executing a smoke run.
- Asset/shader runtime requirements are inferred from startup code paths and project wiring.

---

## 2) Current M1f baseline recap

- Explicit M1f project filter includes core + runtime spine through `Stride.BepuPhysics` and excludes editor/test/sample projects by filter membership. `build/StriV.Engine.Bepu.M1f.slnf`
- Source-built AssemblyProcessor bootstrap is already codified: build AP first, validate artifact payload, then pass AP routing properties into M1f build. `build/striv-build-engine-bepu-m1f.sh`
- Linux/Vulkan routing is explicit via:
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
- Opt-out properties are explicit in M1f build script:
  - `StrideIncludeShaderCompiler=false`
  - `StrideIncludeAudio=false`
  - `StrideIncludeVirtualReality=false`
- What M1f proves: compile closure for selected engine/runtime/Bepu libraries with those opt-outs.
- What M1f does **not** prove: that an executable can initialize/run loop, create context/window/device, or exit cleanly at runtime.

---

## 3) Existing executable/sample survey

### A. Smallest executable pattern found
- **Path:** `samples/Physics/BepuSample/BepuSample.Windows/BepuSampleApp.cs`
- **Entry code:** top-level `using var game = new Game(); game.Run();`
- **Project type:** `WinExe` in companion `.csproj`, `net10.0-windows`, `RuntimeIdentifier=win-x64`. `samples/Physics/BepuSample/BepuSample.Windows/BepuSample.Windows.csproj`
- **Assessment:** minimal **code**, but Windows-only wrapper project and sample stack are not Linux/Vulkan-smoke suitable.

### B. Representative Stride game sample
- **Path:** `samples/Games/JumpyJet/JumpyJet.Windows/JumpyJetApp.cs` + `JumpyJet.Windows.csproj`
- **Project type:** `WinExe`, `net10.0-windows`.
- **Assumptions:** has `StrideCurrentPackagePath` (`.sdpkg`) and Stride project metadata (`StrideProjectType`, `StridePlatform`).
- **Assessment:** demonstrates legacy package/content pipeline assumptions; not suitable as first Stri-V code-first Linux smoke basis.

### C. BepuSample as “too broad” comparison
- **Path:** `samples/Physics/BepuSample/BepuSample.Game/BepuSample.Game.csproj`
- **Dependencies:** package refs include `Stride.Assets`, `Stride.Core.Assets`, `Stride.Core.Assets.CompilerApp`, UI/Video/Particles, etc.
- **Assumptions:** asset compilation/editor-era package flow.
- **Assessment:** useful contrast; too broad for first minimal executable smoke.

---

## 4) Runtime startup API audit

### Minimal startup class and host
- Direct evidence: simple samples instantiate `Stride.Engine.Game` and call `Run()`. `samples/Physics/BepuSample/BepuSample.Windows/BepuSampleApp.cs`
- `Game` derives from `GameBase` and wires major systems (`ScriptSystem`, `SceneSystem`, `Streaming`, optional Audio/VR, etc.). `sources/engine/Stride.Engine/Engine/Game.cs`

### `Game.Run()` requirements
- `Run()` throws if no `IGraphicsDeviceManager` service is present. `sources/engine/Stride.Games/GameBase.cs`
- `Game` initialization adds graphics path and systems through standard initialization flow. `sources/engine/Stride.Engine/Engine/Game.cs`

### Linux context behavior
- If no context provided, `Run()` auto-selects `DesktopSDL` on non-Windows/Android/iOS platforms. `sources/engine/Stride.Games/GameBase.cs`
- `GameContextSDL` exists under `STRIDE_UI_SDL` and binds SDL window abstraction. `sources/engine/Stride.Games/GameContextSDL.cs`
- **Inference:** default Linux path is SDL-backed window context, not WinForms.

### Window/headless
- `Run()` default path creates context and platform run loop; no explicit headless path was found in inspected code.
- **Inference:** first reliable runtime smoke should assume windowed SDL context (even if short-lived), unless a dedicated no-window context is later discovered.

### Default services/systems and opt-outs
- `Game.Initialize()` always adds Input/Script/Scene/Effect/Streaming and registers content serializers.
- Shader compiler hookup is conditional and skipped when `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER` is set.
- Audio and VR systems are conditionally compiled and can be absent with existing M1e flags.
- Therefore, audio/VR/shader-compiler opt-outs are expected not to block **initialization**, but rendering/effects runtime behavior still requires execution to prove.

### Scene requirement
- `Game` constructs `SceneSystem` in constructor; explicit user-created scene is not mandatory for entry-level `Run()` call.
- **Inference:** a first smoke can start with no custom scene content.

### Early exit strategy
- Static-only suggestion: override a `Game` subclass `Update` and call `Exit()` after first tick or short frame counter.
- Not validated here by execution.

---

## 5) Asset/compiler avoidance audit

### Can first executable be code-first?
- **Likely yes**: minimal sample entry `new Game().Run()` itself does not reference `.sdpkg/.sdscene/.sdproj`.
- Legacy sample wrappers (e.g., JumpyJet) do reference `.sdpkg`, but this is project-specific, not intrinsic to `Game` startup.

### Content database / assets
- `GameBase.PrepareContext()` always creates a `ContentManager`.
- `Game` has asset-database helper code (`InitializeAssetDatabase`), suggesting content infra exists in runtime path.
- **Inference:** content services exist by default, but a smoke app may avoid explicit asset loads and still potentially run.

### Shader source compilation
- Engine supports compile-time opt-out via `StrideIncludeShaderCompiler=false` → `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER` and removes compiler source files/project reference.
- **Inference:** legacy source shader compiler path can be avoided for first smoke.

### Game Studio/editor pipeline
- Nothing in minimal entrypoint requires Game Studio directly.
- Avoiding `Stride.Assets` and `Stride.Core.Assets.CompilerApp` references in the smoke project should avoid editor/asset-compiler pipeline coupling.

### Headless/no-render
- No verified headless context found in this pass.
- If rendering path still expects built-in effects/content, compile-first executable is safer as first implementation.

---

## 6) Proposed first executable target

### Recommended target (smallest credible)
- **Project path:** `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`
- **Output type:** `Exe` (not WinExe)
- **Target framework:** `net10.0`
- **References:** direct `ProjectReference` to `sources/engine/Stride.Engine/Stride.Engine.csproj` (which transitively brings M1f spine).
- **No references** to `Stride.Assets`, `Stride.Core.Assets`, editor/presentation, sample packages.

### Filter/script proposal
- New filter: `build/StriV.CoreSmoke.M1g.slnf`
- Include M1f project list + new smoke executable project.
- New script: `build/striv-build-coresmoke-m1g.sh` (and optional `.ps1`) using same AP source-built routing pattern as M1f script.

### Minimal code proposal
- `Program.cs`: instantiate custom subclass `CoreSmokeGame : Game` or direct `Game`.
- Prefer subclass with deterministic early `Exit()` in first update to constrain runtime risk.
- No asset loads, no scene files, no audio, no VR, no shader compiler.

---

## 7) Candidate implementation options

### Option A — compile-only executable
- **Projects:** new smoke exe + M1f spine via project refs/filter.
- **Blockers:** mostly build graph wiring only.
- **Proves:** executable project can compile against M1f runtime spine.
- **Does not prove:** runtime initialization or SDL/Vulkan execution.
- **Recommendation:** **Primary first step** (lowest risk, highest signal for M1g-prep).

### Option B — headless runtime smoke
- **Projects:** same as A.
- **Blockers:** uncertain headless context support.
- **Proves:** runtime init loop without window (if possible).
- **Does not prove:** SDL/Vulkan presentation path.
- **Recommendation:** secondary only if concrete headless context is identified.

### Option C — SDL/Vulkan window smoke
- **Projects:** same as A.
- **Blockers:** SDL/Vulkan native runtime/environment availability.
- **Proves:** real runtime loop + window/device path on Linux.
- **Does not prove:** asset/content-heavy paths.
- **Recommendation:** do after Option A compile closure; optionally gated by explicit run flag.

### Option D — Bepu-only code smoke
- **Projects:** could reference Bepu libraries directly.
- **Blockers:** may bypass `Game` runtime entirely.
- **Proves:** Bepu API usability in isolation.
- **Does not prove:** strategic goal (“tiny Stri-V Core executable can launch”).
- **Recommendation:** not sufficient as first executable objective.

---

## 8) Proposed solution filter/build script

### Recommended artifacts
- `.slnf`: `build/StriV.CoreSmoke.M1g.slnf`
- Script: `build/striv-build-coresmoke-m1g.sh` (plus optional PS1 parity)

### Explicit project list
- Start with exact M1f list from `build/StriV.Engine.Bepu.M1f.slnf`.
- Add `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`.

### Required properties
- `StridePlatforms=Linux`
- `StrideGraphicsApis=Vulkan`
- `StrideIncludeShaderCompiler=false`
- `StrideIncludeAudio=false`
- `StrideIncludeVirtualReality=false`
- `StrideAssemblyProcessorFramework=net10.0`
- `StrideAssemblyProcessorBasePath=<source-built AP output>`
- `StrideAssemblyProcessorHash=sourcebuild`

### Build vs run
- **Recommended next implementation default:** build only.
- Optional run mode can be added but should be opt-in and stop/report at first blocker.

---

## 9) Risk register

| Risk | Area | Likelihood | Impact | Evidence | Mitigation |
| ---- | ---- | ---------: | -----: | -------- | ---------- |
| Game startup requires asset/content DB semantics beyond empty code-first run | Runtime init | Medium | High | `GameBase` always creates `ContentManager`; `Game` contains asset DB init helpers | Start with compile-only, then runtime smoke with no explicit content loads |
| Game startup requires shader compiler | Rendering/effects | Low-Med | High | Shader compiler integration is compile-time optional in `Stride.Engine.csproj` and guarded in `Game.cs` | Keep `StrideIncludeShaderCompiler=false`; avoid effect/material customization |
| Audio/VR still required despite opt-outs | Runtime systems | Low | Med | Conditional compile and project refs in `Stride.Engine.csproj`/`Game.cs` | Keep opt-out flags in all smoke builds/runs |
| SDL/Vulkan native runtime unavailable in environment | Runtime host | Medium | High | Linux path defaults to DesktopSDL in `Run()` | Make run optional; report environment blockers distinctly |
| Rendering path needs precompiled effects/content | Rendering | Medium | Medium-High | EffectSystem exists by default; exact runtime behavior unproven statically | Exit quickly; defer render-heavy validation |
| no-audio/no-VR API assumptions still touched by scripts/content | Engine behavior | Low-Med | Medium | Existing M1e already required no-audio guard work | Keep smoke content-free; avoid audio/VR APIs in smoke code |
| Smoke exe accidentally pulls editor/assets/samples | Build graph | Medium | Medium | Many samples depend on assets compiler packages | Use direct project refs to engine runtime only |
| Bepu sample baseline is too broad and Windows-centric | Scope control | High | Medium | `BepuSample` uses windows wrapper and asset packages | Use it only as contrast, not implementation base |

---

## 10) Recommended implementation prompt (for next Codex task)

Create the first **M1g compile-first executable smoke slice** only.

1. Add `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj` and `Program.cs` with minimal Stride runtime entry (`Game` or tiny subclass), no assets/content files, no `.sdpkg/.sdscene/.sdproj`.
2. Add `build/StriV.CoreSmoke.M1g.slnf` containing the exact M1f project list plus the new smoke project.
3. Add `build/striv-build-coresmoke-m1g.sh` (and optional `.ps1` parity) reusing M1f source-built AssemblyProcessor bootstrap and routing:
   - `StridePlatforms=Linux`
   - `StrideGraphicsApis=Vulkan`
   - `StrideIncludeShaderCompiler=false`
   - `StrideIncludeAudio=false`
   - `StrideIncludeVirtualReality=false`
   - `StrideAssemblyProcessorFramework=net10.0`
   - `StrideAssemblyProcessorBasePath` from source build
   - `StrideAssemblyProcessorHash=sourcebuild`
4. Default behavior: **build only**. If a run mode is added, make it explicit/opt-in.
5. Do not touch editor/assets/sdk targets beyond what is required for this project/filter/script.
6. Stop at first blocker and report exact command, error, and dependency edge causing it.

