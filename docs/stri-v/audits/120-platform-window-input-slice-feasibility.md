# 120 – Platform/window/input slice feasibility audit (M1c-prep)

## 1) Evidence collection

### Commands used
- `rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideExplicitWindowsRuntime|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|Microsoft.WindowsDesktop.App|StridePlatform|StridePlatforms|StrideGraphicsApis|SDL|Sdl|GameWindowSDL|GameFormSDL|SDLMessageLoop|Desktop|Linux|Windows|Win32|WPF|WinForms|Direct3D|Vulkan|Graphics|Input|Keyboard|Mouse|GamePad|Touch|Android|iOS|UWP|OpenTK|Silk" sources/engine/Stride.Games sources/engine/Stride.Input sources/engine/Stride.Graphics sources/sdk sources/targets`
- `find sources/engine/Stride.Games -maxdepth 3 -type f | sort`
- `find sources/engine/Stride.Input -maxdepth 3 -type f | sort`
- `sed -n '1,260p'` on:
  - `sources/engine/Stride.Games/Stride.Games.csproj`
  - `sources/engine/Stride.Input/Stride.Input.csproj`
  - `sources/engine/Stride.Graphics/Stride.Graphics.csproj`
- `sed -n` on SDK logic:
  - `sources/sdk/Stride.Build.Sdk/Sdk/Sdk.props`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.props`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.targets`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Frameworks.props`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Frameworks.targets`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Graphics.props`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Graphics.targets`
  - `sources/sdk/Stride.Build.Sdk/Sdk/Stride.GraphicsApi.InnerBuild.targets`
- `rg -n "STRIDE_UI_|STRIDE_PLATFORM_|#if|#elif|#endif|System.Windows.Forms|Windows\.UI|Android|iOS|UWP|Silk.NET" sources/engine/Stride.Games sources/engine/Stride.Input`
- `rg -n "M1b|engine-foundation|AssemblyProcessorBasePath|sourcebuild|slnf" docs/stri-v -g '*.md'`

### Search terms used
Exactly the terms requested by task plus focused preprocessor-symbol terms (`STRIDE_UI_*`, `STRIDE_PLATFORM_*`).

### Files opened/read
See command list above; primary audited files were the three candidate `.csproj` and the SDK props/targets controlling target frameworks/platform/API dispatch.

### Scripts used
No custom parsing script needed; evidence extracted from `rg`, `find`, and `sed` only.

### Mutation confirmation
Static audit only. No source/project/targets files were modified during evidence gathering. (This document creation is the only repository change.)

### Static-only uncertainty
No restore/build/test was run per constraint. Any compile viability conclusions are feasibility-level and require follow-up execution validation.

---

## 2) Current M1b baseline recap

- M1b explicit projects: six M1a core projects plus `sources/engine/Stride/Stride.csproj` (with transitive `Stride.FreeImage`) per existing M1b docs.
- M1b bootstrap pattern: source-build `Stride.Core.AssemblyProcessor`, then pass:
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<absolute dir with trailing slash>`
  - `StrideAssemblyProcessorHash=sourcebuild`
- M1b proves Linux Debug/Release compile feasibility for foundational engine layer.
- M1b does **not** prove platform/window/input/graphics stack viability; that layer remains unvalidated.

---

## 3) `Stride.Games` project audit

### Direct references
- `ProjectReference`: `../Stride.Graphics/Stride.Graphics.csproj` (hard dependency).
- No direct package references in `Stride.Games.csproj` itself.

### Key properties
- `StrideRuntime=true`
- `StrideExplicitWindowsRuntime=true`
- `StrideGraphicsApiDependent=true`
- `StrideAssemblyProcessor=true`
- `StrideAssemblyProcessorOptions=--auto-module-initializer`
- `UseWPF=true` and `UseWindowsForms=true` only when `$(TargetFramework.Contains('-windows'))`

### Target framework behavior
From SDK logic:
- `StrideRuntime=true` always includes `net10.0`.
- `net10.0-windows` is only added when `StridePlatforms` contains `Windows` **and** `StrideExplicitWindowsRuntime=true`.
- On Linux host default, `StridePlatforms` defaults to `Linux`, so `net10.0-windows` should not be added unless explicitly overridden.

### Answers
1. **Can add on Linux without WindowsDesktop in practice?**
   - **Plausibly yes**, if `StridePlatforms=Linux` (or default Linux detection) is honored and no Windows target framework is introduced.
2. **Does it require `Stride.Graphics`?**
   - **Yes, directly.**
3. **Does that imply first graphics slice too?**
   - **Yes.** M1c with `Stride.Games` is inherently platform+graphics-basics.
4. **Graphics API variants required?**
   - `StrideGraphicsApiDependent=true`; SDK dispatches per API. On Linux defaults point to Vulkan.
5. **Linux TFM plausibility**
   - Plausible `net10.0` only; not necessarily `net10.0-windows` if Windows is excluded from `StridePlatforms`.
6. **Command-line mitigation**
   - Force: `-p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan`.

**Feasibility rating:** **Medium** (strong structural plausibility, but compile unproven).

**Recommendation:** include with explicit Linux/Vulkan properties; treat as platform+graphics-basics slice.

---

## 4) `Stride.Games` platform source audit

### SDL group
- Representative files: `SDL/GameFormSDL.cs`, `SDL/GameWindowSDL.cs`, `SDL/SDLMessageLoop.cs`, `GameContextSDL.cs`.
- Compile guards: `#if STRIDE_UI_SDL`.
- Purpose: desktop/mobile SDL window/message-loop/context path.
- Linux relevance: high.
- Risk: moderate (depends on UI define selection and `Stride.Graphics.SDL` availability via graphics layer).

### Desktop WinForms/WPF/Win32 group
- Representative files: `Desktop/GameWindowWinforms.cs`, `Desktop/GameForm.cs`, `Desktop/WindowsMessageLoop.cs`, `Desktop/Win32Native.cs`, `GameContextWinforms.cs`.
- Guards: `#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)` and variants.
- Uses `System.Windows.Forms` directly.
- Linux relevance: low.
- Risk: medium if wrong UI symbol set occurs.

### UWP group
- Representative files: `WindowsStore/GamePlatformUWP.cs`, `WindowsStore/GameWindowUWP.cs`, `GameContextUWP.cs`.
- Guards: `#if STRIDE_PLATFORM_UWP`.
- Linux relevance: none.
- Risk: low if platform defines are correct.

### Android/iOS group
- Representative files: `Android/GamePlatformAndroid.cs`, `GameContextAndroid.cs`, `Starter/StrideActivity.cs`, `iOS/GamePlatformiOS.cs`, `GameContextiOS.cs`.
- Guards: `#if STRIDE_PLATFORM_ANDROID` / `#if STRIDE_PLATFORM_IOS`.
- Linux relevance: none for M1c.
- Risk: low if non-mobile TFM only.

### Entanglement conclusions
- SDL path is real and substantial.
- SDL path is not inherently WinForms/WPF-bound in source; it is symbol-gated separately.
- WinForms/WPF files appear compile-excluded unless UI symbols select them.
- Project split is **not yet clearly required**; first try property-constrained build.

---

## 5) `Stride.Input` project audit

### Direct references/packages
- `ProjectReference`: `../Stride.Games/Stride.Games.csproj` (thus indirect graphics coupling).
- Windows-only package refs conditioned on `TargetFramework.Contains('-windows')`:
  - `SharpDX.DirectInput`
  - `SharpDX.XInput`
  - `Microsoft.Management.Infrastructure`

### Properties
- Same key toggles as Games: `StrideRuntime=true`, `StrideExplicitWindowsRuntime=true`, `StrideGraphicsApiDependent=true`, `StrideAssemblyProcessor=true`, conditional `UseWPF`/`UseWindowsForms`.

### Source evidence
- SDL input path exists (`SDL/InputSourceSDL.cs`, `KeyboardSDL.cs`, `MouseSDL.cs`, etc.) under `#if STRIDE_UI_SDL`.
- Windows input path exists under `Windows/*` with `#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)` or raw-input-specific defines.
- Mobile/UWP paths exist but symbol-gated.

### Feasibility + staging
- Feasible on Linux in principle if Windows TFM avoided and SDL UI symbols selected.
- But it increases risk surface materially (more platform condition complexity + extra packages on windows TFM).

**Recommendation:** defer `Stride.Input` to **M1d** unless M1c goals explicitly require input.

**Feasibility rating:** **Medium/Unknown** (structurally plausible; compile not run).

---

## 6) `Stride.Graphics` coupling check

- Adding `Stride.Games` necessarily adds `Stride.Graphics` (direct project reference).
- `Stride.Graphics` directly references:
  - `Stride.Shaders`
  - `Stride`
  - `Stride.Core.Tasks` (non-reference output usage)
- `Stride.Graphics` package/native indicators:
  - Vulkan: `Vortice.Vulkan`
  - D3D: `Silk.NET.Direct3D11`, `Silk.NET.Direct3D12`, `Silk.NET.Direct3D.Compilers`, `WinPixEventRuntime` (windows-target conditions)
  - SDL: `Silk.NET.Sdl`
  - Native libs: freetype always; MoltenVK on Vulkan condition.
- Therefore M1c with Games is effectively first graphics layer.

**Naming implication:** consider naming as **platform + graphics-basics** slice.

---

## 7) SDK/platform property behavior

- `StridePlatforms` default is OS-derived in `Stride.Platform.props` (Linux host => `Linux` when unset).
- `StrideRuntime=true` computes `TargetFrameworks` in `Stride.Frameworks.targets`:
  - Always includes `net10.0`
  - Adds `net10.0-windows` only when platforms include Windows and explicit-windows-runtime is true.
- `StrideGraphicsApis` defaults:
  - Linux => `Vulkan`
  - Windows => `Direct3D11` (or multi-API in graphics targets depending context)
- `StrideExplicitWindowsRuntime=true` only has effect if `StridePlatforms` includes Windows.
- Setting `-p:StridePlatforms=Linux` should avoid windows TFM expansion.
- For clarity and repeatability, pass both `StridePlatforms=Linux` and `StrideGraphicsApis=Vulkan` explicitly.

---

## 8) Candidate slice options

### Option A: M1b + `Stride.Games` only
- Explicit: M1b projects + `Stride.Games`.
- Transitive likely: `Stride.Graphics`, `Stride.Shaders`, etc.
- Blockers: hidden graphics coupling still present.
- Proves: platform/window layer entry plus first graphics coupling reality.
- Does not prove: input stack.
- Recommendation: acceptable if documented as platform+graphics-basics.

### Option B: M1b + `Stride.Games` + explicit `Stride.Graphics`
- Explicitly acknowledges real dependency.
- Better audit transparency than A.
- Proves: minimal viable Games+Graphics slice compileability.
- Recommendation: **preferred** minimal honest next step.

### Option C: M1b + `Stride.Games` + `Stride.Graphics` + `Stride.Input`
- Bigger risk surface (input platform branches, extra symbols, windows-only packages under windows TFM).
- Proves more at once, but violates smallest-slice doctrine.
- Recommendation: defer unless B succeeds first.

### Option D: split/refactor `Stride.Games` first
- Requires code/project surgery before validation.
- Not smallest immediate step.
- Recommendation: fallback only if B shows hard coupling blockers.

---

## 9) Proposed next implementation target

**Smallest credible target:** Option B.

- Proposed `.slnf`: `build/StriV.PlatformGraphicsBasics.M1c.slnf`
- Explicit projects to include:
  - All projects currently in `build/StriV.EngineFoundation.M1b.slnf`
  - `sources/engine/Stride.Games/Stride.Games.csproj`
  - `sources/engine/Stride.Graphics/Stride.Graphics.csproj`
- Bootstrap script:
  - Mirror existing M1b script pattern (new M1c-specific wrapper preferred for isolation).
- Restore/build command pattern (future execution task):
  - `dotnet restore build/StriV.PlatformGraphicsBasics.M1c.slnf -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath=<abs_ap_dir_with_trailing_slash> -p:StrideAssemblyProcessorHash=sourcebuild`
  - `dotnet build build/StriV.PlatformGraphicsBasics.M1c.slnf -c Debug -v minimal` with same AP/platform properties.
- Expected first blockers (prediction):
  - graphics/shader transitive compile or packaging assumptions,
  - SDL/UI define resolution mismatches,
  - native dependency resolution (Vulkan/SDL/freetype/MoltenVK handling).

---

## 10) Risk register

| Risk | Candidate/project | Likelihood | Impact | Evidence | Mitigation |
| ---- | ----------------- | ---------: | -----: | -------- | ---------- |
| `Stride.Games` forces `Stride.Graphics` | Games | High | High | Direct ProjectReference in csproj | Include Graphics explicitly in M1c plan. |
| `Stride.Games` forces `net10.0-windows` | Games | Medium | High | `StrideExplicitWindowsRuntime=true`, but Windows TFM depends on `StridePlatforms` | Force `StridePlatforms=Linux` in commands/scripts. |
| WPF/WinForms conditions misfire on Linux | Games/Input | Medium | High | Conditional `UseWPF/UseWindowsForms`, many WinForms-gated files | Keep Linux-only platforms + verify preprocessed symbol behavior in build step. |
| SDL files exist but wrong include path at build | Games/Input | Medium | Medium | SDL files guarded by `STRIDE_UI_SDL`; symbol source indirect | Validate with first M1c compile, capture first symbol-related failure. |
| `Stride.Graphics` pulls shader/compiler complexity early | Graphics | High | High | Direct ref to `Stride.Shaders`; API-dependent build model | Accept as unavoidable for Games; keep slice limited otherwise. |
| `Stride.Graphics` pulls D3D/native deps | Graphics | Medium | Medium | D3D packages conditional; Vulkan/SDL/native libs present | Pin Linux+Vulkan and avoid Windows TFM. |
| `Stride.Input` pulls Windows-only packages | Input | Medium | Medium | Conditional package refs on windows TFM | Defer Input to M1d or ensure no windows TFM. |
| Mobile files leak into slice | Games/Input | Low | Medium | Android/iOS/UWP sources behind platform symbols | Keep `StridePlatforms=Linux`; no mobile TFMs. |
| AssemblyProcessor routing still required | All candidate runtime projects | High | High | AP enabled in csproj + prior M1a/M1b pattern | Reuse source-built AP properties/scripts. |
| AP scripts need generalization/duplication | Build scripts | Medium | Medium | Prior M1b uses dedicated scripts | Add dedicated M1c wrapper mirroring M1b flow. |

---

## 11) Recommended implementation prompt (next task)

> Create only `build/StriV.PlatformGraphicsBasics.M1c.slnf` as the next slice above M1b by including the existing M1b explicit projects plus `sources/engine/Stride.Games/Stride.Games.csproj` and `sources/engine/Stride.Graphics/Stride.Graphics.csproj` (and no editor/assets/presentation/mobile/Stride.Engine additions). If needed, add dedicated M1c bootstrap script(s) mirroring M1b’s source-built AssemblyProcessor flow: build `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`, validate AP output payload, then restore/build only the new M1c filter using `-p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath=<absolute AP output dir with trailing slash> -p:StrideAssemblyProcessorHash=sourcebuild`. Run Debug first, do not run full solution builds/tests, do not patch unrelated projects/targets, and report the first blockers precisely with file-path evidence.

