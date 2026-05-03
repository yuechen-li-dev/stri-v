# 100 – Engine foundation slice feasibility (static audit)

## 1) Evidence collection

### Commands run
- `pwd`
- `rg -n --files -g 'AGENTS.md'`
- `find .. -name AGENTS.md -print`
- `rg -n "StriV.Core.M1a.slnf|striv-build-core-m1a|AssemblyProcessor" build docs/stri-v -g "*.md" -g "*.sh" -g "*.ps1"`
- `rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideExplicitWindowsRuntime|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|StridePlatform|StridePlatforms|StrideGraphicsApis|Assets|Compiler|Quantum|Editor|Presentation|GameStudio|Shader|Shading|SDSL|FreeImage|Image|ContentManager|Content|Serialization|Graphics|Rendering|Audio|Input|VirtualReality|VR|Physics|Bepu|Navigation|Particles|Video|Voxels|SpriteStudio" sources/engine sources/core sources/shaders sources/assets sources/tools`
- `sed -n '1,260p' sources/engine/Stride/Stride.csproj; sed -n '1,320p' sources/engine/Stride.Engine/Stride.Engine.csproj`
- `cat build/StriV.Core.M1a.slnf; sed -n '1,240p' sources/sdk/Stride.Build.Sdk/Sdk/Sdk.props; sed -n '1,260p' sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets; sed -n '1,220p' sources/tools/Stride.FreeImage/Stride.FreeImage.csproj; sed -n '1,220p' sources/engine/Stride.Audio/Stride.Audio.csproj; sed -n '1,260p' sources/engine/Stride.Rendering/Stride.Rendering.csproj; sed -n '1,220p' sources/engine/Stride.VirtualReality/Stride.VirtualReality.csproj; sed -n '1,220p' sources/engine/Stride.Shaders.Compiler/Stride.Shaders.Compiler.csproj`
- `nl -ba ...` on all evidence csproj/targets/slnf/doc files for line-anchored citations.

### Search terms used
Used exactly the user-provided composite pattern plus focused terms (`AssemblyProcessor`, `striv-build-core-m1a`, `StriV.Core.M1a.slnf`).

### Files opened/read
- `sources/engine/Stride/Stride.csproj`
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/engine/Stride.Audio/Stride.Audio.csproj`
- `sources/engine/Stride.Rendering/Stride.Rendering.csproj`
- `sources/engine/Stride.VirtualReality/Stride.VirtualReality.csproj`
- `sources/engine/Stride.Shaders.Compiler/Stride.Shaders.Compiler.csproj`
- `sources/tools/Stride.FreeImage/Stride.FreeImage.csproj`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`
- `build/StriV.Core.M1a.slnf`
- `build/striv-build-core-m1a.sh`
- `docs/stri-v/building-core.md`

### Parsing scripts used
No custom parser script was needed; direct csproj inspection was sufficient for direct dependency evidence.

### File modification confirmation
This is a **static-only audit**. No source/project/targets/filter files were edited as part of feasibility analysis; only this new audit report was added.

### Static-only uncertainty
Transitive graph breadth/risk is inferred from direct project references and well-known subtree names; precise full transitive closure was not computed via build graph tooling in this pass.

---

## 2) Current M1a baseline recap

- M1a includes six core projects: `Stride.Core`, `Stride.Core.Mathematics`, `Stride.Core.IO`, `Stride.Core.MicroThreading`, `Stride.Core.Serialization`, `Stride.Core.Reflection`. (`build/StriV.Core.M1a.slnf`) 
- M1a bootstrap script builds `Stride.Core.AssemblyProcessor` first, validates payload, then builds M1a with `StrideAssemblyProcessorFramework=net10.0`, `StrideAssemblyProcessorBasePath=<source build output>`, `StrideAssemblyProcessorHash=sourcebuild`. 
- This proves the source-built AssemblyProcessor routing pattern works for a minimal core-managed slice on Linux.
- It explicitly does **not** prove rendering/window/input/audio/physics/asset compiler/editor readiness.

---

## 3) Candidate project audit: `sources/engine/Stride/Stride.csproj`

- **Purpose (inferred):** foundational engine runtime library named `Stride`; appears lighter than `Stride.Engine` and directly ties to serialization/mathematics + FreeImage.
- **Direct project refs:**
  - `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
  - `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
  - `sources/tools/Stride.FreeImage/Stride.FreeImage.csproj`
- **Transitive notes (static/inference):** FreeImage project references `Stride.Core`; likely stays mostly core/tooling-level but introduces native library packaging.
- **Package refs:** `System.Drawing.Common` conditionally on `$(StrideFramework)`.
- **Target framework behavior:** no explicit `TargetFramework` in file; inherited from SDK/props.
- **`StrideRuntime`:** `true`.
- **`StrideAssemblyProcessor`:** `true`, with options `--auto-module-initializer --serialization`.
- **`StrideGraphicsApiDependent`:** not set in this project.
- **WPF/WinForms/WindowsDesktop coupling:** none directly observed.
- **Asset/compiler/Quantum/editor/presentation coupling:** no direct project refs to those systems.
- **Graphics/rendering/window/input coupling:** no direct refs to `Stride.Graphics`, `Stride.Rendering`, `Stride.Input`.
- **Native dependency coupling:** yes, via `Stride.FreeImage` packaging of native DLL payloads from `deps/FreeImage/Release/**`.
- **FreeImage/content/serialization specifics:** direct `Stride.FreeImage` ref and `Stride.Core.Serialization` ref present.
- **Linux feasibility:** **Medium** (credible next step, but native packaging path and platform-specific runtime layout can be a blocker).
- **Recommendation:** **Include in next slice** (smallest step above M1a).
- **Evidence paths:** see cited files.

---

## 4) Candidate project audit: `sources/engine/Stride.Engine/Stride.Engine.csproj`

- **Purpose (inferred):** broader engine facade/runtime assembly.
- **Direct project refs:**
  - `sources/engine/Stride.Audio/Stride.Audio.csproj`
  - `sources/engine/Stride.Rendering/Stride.Rendering.csproj`
  - `sources/engine/Stride.VirtualReality/Stride.VirtualReality.csproj`
  - `sources/engine/Stride.Shaders.Compiler/Stride.Shaders.Compiler.csproj`
- **Transitive notes (static/inference):**
  - `Stride.Rendering` → `Stride.Games`.
  - `Stride.VirtualReality` sets `StrideGraphicsApiDependent=true`, references `Stride.Games`, `Stride.Graphics`, `Stride.Input`, and ships OpenVR/OpenXR native libs for specific graphics API conditions.
  - `Stride.Shaders.Compiler` touches Windows SDK registry and D3D/glslang native payloads.
  - `Stride.Audio` has native output (`libstrideaudio`), native source files, and references `Stride.Native` plus `Stride`.
- **Package refs:** `System.ValueTuple`, `System.Threading.Tasks.Dataflow` plus transitive packages from subprojects (including Silk.NET VR/D3D compiler packages).
- **Target framework behavior:** no explicit TFM in file; inherited. Subprojects introduce platform-conditional behavior.
- **`StrideRuntime`:** `true`.
- **`StrideAssemblyProcessor`:** `true` with `--parameter-key --auto-module-initializer --serialization`.
- **`StrideGraphicsApiDependent`:** not on `Stride.Engine` itself, but present in direct dependency `Stride.VirtualReality`.
- **WPF/WinForms/WindowsDesktop coupling:** not direct, but Windows SDK registry and D3D coupling exist in direct deps.
- **Asset/compiler/Quantum/editor/presentation coupling:** no direct editor ref, but `StridePackAssets=true` and shader compiler coupling is explicit.
- **Graphics/rendering/window/input coupling:** **Yes, strong** via direct refs.
- **Shader compiler/parser coupling:** **Yes, explicit** via `Stride.Shaders.Compiler` (and parser transitively).
- **Audio/rendering/VR/navigation/physics coupling:** audio/rendering/VR are direct.
- **Linux feasibility:** **Low** for “next smallest” slice; too broad and likely violates add-one-layer doctrine.
- **Recommendation:** **Defer/exclude for immediate next slice**.
- **Evidence paths:** see cited files.

---

## 5) Dependency graph comparison

| Candidate | Direct project refs | Pulls graphics? | Pulls rendering? | Pulls shader compiler? | Pulls assets/compiler? | Pulls editor/presentation? | Pulls native deps? | Expected Linux risk | Recommendation |
| --------- | ------------------- | --------------- | ---------------- | ---------------------- | ---------------------- | -------------------------- | ------------------ | ------------------- | -------------- |
| `Stride.csproj` | Core.Serialization, Core.Mathematics, FreeImage | No direct | No direct | No | No direct | No direct | Yes (FreeImage native payload) | Medium | **Next slice include** |
| `Stride.Engine.csproj` | Audio, Rendering, VirtualReality, Shaders.Compiler | Yes | Yes | Yes | Shader compiler/toolchain | Not direct editor, but broad runtime/tooling surface | Yes (audio/VR/shader native payloads) | High | **Defer** |

---

## 6) Next-slice proposal

### Proposed smallest credible slice above M1a
**Outcome:** `sources/engine/Stride/Stride.csproj` is the smallest viable increment; `Stride.Engine.csproj` is too broad.

### Proposed future filter name
- `build/StriV.EngineFoundation.M1b.slnf` (not created in this task).

### Exact project paths to include
- Existing M1a projects:
  - `sources/core/Stride.Core/Stride.Core.csproj`
  - `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
  - `sources/core/Stride.Core.IO/Stride.Core.IO.csproj`
  - `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
  - `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
  - `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj`
- Plus:
  - `sources/engine/Stride/Stride.csproj`
- Expect transitive pull of:
  - `sources/tools/Stride.FreeImage/Stride.FreeImage.csproj`

### Explicitly excluded systems
- `Stride.Engine` aggregate layer and its direct pulls: `Stride.Audio`, `Stride.Rendering`, `Stride.VirtualReality`, `Stride.Shaders.Compiler`.
- Rendering/graphics/window/input/VR/shader compilation/editor/toolchain slices remain out.

### Expected first build command (future implementation task)
- `dotnet build build/StriV.EngineFoundation.M1b.slnf -c Debug -v minimal -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath=<ap_output_dir_with_trailing_slash> -p:StrideAssemblyProcessorHash=sourcebuild`

### Expected blockers
- AssemblyProcessor remains mandatory and must be source-built/routed.
- FreeImage native payload/runtime packaging could produce Linux-specific issues.
- Potential transitive dependency surprises from `Stride.FreeImage` or inherited SDK props.

### What this slice proves
- Core + `Stride` assembly can compile with source-built AP bootstrap.
- Initial engine-foundation expansion without rendering/audio/input/VR stack.

### What it does not prove
- Rendering, graphics API, shader compile pipeline, audio backend, VR runtime, editor/content pipeline readiness.

---

## 7) Bootstrap script strategy

**Recommendation:** keep scripts separate for now.

- Add a dedicated future script (`build/striv-build-engine-foundation-m1b.sh` and parity `.ps1`) mirroring M1a flow.
- Rationale: avoids premature abstraction/generalization while M1b slice is still being validated and may need bespoke diagnostics.

---

## 8) Runtime/content boundary classification

| System | Classification | Notes |
| ------ | -------------- | ----- |
| `ContentManager` | required later | Not directly required by `Stride.csproj` evidence in this pass. |
| runtime content serialization | required now | `Stride` opts into serialization AP options and references Core.Serialization. |
| `ObjectUrl` / object database | unknown | Not proven from inspected candidate files. |
| source asset YAML | excluded (for now) | Outside proposed slice. |
| `Package/PackageSession` | excluded (for now) | Asset system layer, not required for this increment. |
| Quantum | excluded | Outside core runtime doctrine for this slice. |
| AssetCompiler | excluded | Outside next engine-foundation step. |
| shader compiler/parser | required later | Explicitly in `Stride.Engine` broad slice, excluded for M1b. |
| texture/font/model toolchain | required later | Typically coupled with rendering/content pipeline, excluded now. |
| FreeImage | required now (for `Stride.csproj`) | Direct project ref from `Stride` candidate. |

---

## 9) Linux feasibility risk register

| Risk | Candidate/project | Likelihood | Impact | Evidence | Mitigation |
| ---- | ----------------- | ---------: | -----: | -------- | ---------- |
| AssemblyProcessor still required | `Stride` + all runtime csproj with AP=true | High | High | AP enabled in both candidates; AP target wiring requires resolved processor path | Reuse M1a source-built AP bootstrap properties exactly. |
| Native dependency through FreeImage | `Stride` via `Stride.FreeImage` | Medium | Medium/High | `Stride` directly references FreeImage; FreeImage packages native DLL payloads | Keep scope narrow; validate restore/build first; defer runtime execution claims. |
| `Stride.Engine` pulls shader/render/audio/VR too early | `Stride.Engine` | High | High | Direct refs to Audio/Rendering/VirtualReality/Shaders.Compiler | Exclude from next slice; stage later milestones. |
| Source asset pipeline leaks into foundation | `Stride.Engine` transitive | Medium | Medium | `StridePackAssets=true` on `Stride.Engine`/`Stride.Rendering` | Stay with `Stride` only for M1b. |
| Graphics/window/input leak into foundation | `Stride.Engine` transitive | High | High | `Stride.VirtualReality` references Graphics/Input and graphics-api-dependent behavior | Exclude `Stride.Engine`. |
| Linux native package availability | FreeImage/VR/shader tool deps | Medium | High | Native libs referenced from deps trees and Windows-specific artifacts in some projects | Avoid broad slice; handle native deps incrementally. |
| Solution filter misses transitive deps | Proposed M1b filter | Medium | Medium | Only static audit done, no build executed here | In next task, run restore/build for new filter and capture first blockers only. |
| Build script property routing needs extension | M1b bootstrap | Medium | Medium | M1a script hardcodes M1a slnf path | Create separate M1b script cloned from proven M1a pattern. |

---

## 10) Recommended implementation prompt (for next Codex task)

> Create `build/StriV.EngineFoundation.M1b.slnf` as the smallest slice above M1a by including all projects from `build/StriV.Core.M1a.slnf` plus `sources/engine/Stride/Stride.csproj` (and only those explicit entries in the filter). Add bootstrap script(s) only if needed (`build/striv-build-engine-foundation-m1b.sh` and optional `.ps1`) by mirroring the existing M1a source-built AssemblyProcessor flow: first build `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`, validate produced DLL payload, then build the new slnf with `-p:StrideAssemblyProcessorFramework=net10.0`, `-p:StrideAssemblyProcessorBasePath=<absolute AP output dir with trailing slash>`, and `-p:StrideAssemblyProcessorHash=sourcebuild`. Run restore/build only for the new M1b filter (Debug first), do not run full solution builds/tests, do not edit unrelated source/targets/projects, and report blockers precisely with first-failure evidence.
