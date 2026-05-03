# 040 – Stri-V Core Slice Feasibility (Static M1a Prep Audit)

## 1) Evidence collection

### Scope and constraints followed
- Static analysis only: inspected project files, build targets, and prior audits.
- No full builds, no tests, no solution filter creation, no code/project edits.
- No AssemblyProcessor or target-framework patching attempted.

### Shell commands used
- `rg --files -g '*.csproj' sources build samples > /tmp/csproj_files.txt`
- `rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideExplicitWindowsRuntime|StrideAssemblyProcessor|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|Microsoft.WindowsDesktop.App|StridePlatform|StridePlatforms|StrideGraphicsApis|SDL|Vulkan|Direct3D|Quantum|Assets|Compiler|Editor|Presentation|GameStudio|VisualScript|Video|Voxels|SpriteStudio|Bepu|Bullet" sources build samples > /tmp/audit_rg.txt`
- `rg --files sources -g 'Stride*.csproj' | rg 'sources/(core|engine|graphics|input|games|audio|physics|navigation|particles|ui|rendering|assets|editor|presentation|tools|shaders)/'`
- Python one-off parser (stdin script) to enumerate per-candidate csproj properties/references into `/tmp/candidate_meta.txt`
- `sed -n` reads on: prior audit 030, `build/Stride.Runtime.slnf`, `sources/targets/Stride.targets`, and key candidate `.csproj` files.
- `rg -n "sources/assets/|sources/editor/|sources/presentation/|Stride.Shaders.Compiler|Stride.Shaders.Parser|Stride.Games|Stride.Input|Stride.Graphics|Stride.Rendering|Stride.Engine" build/Stride.Runtime.slnf`

### Search terms used
- Exactly the terms requested in prompt (project/package refs, framework/runtime flags, graphics APIs, editor/assets/Quantum/compiler/tooling markers, platform markers).

### Files opened/read (primary evidence)
- Prior audits: `docs/stri-v/audits/030-asset-serialization-quantum-assemblyprocessor.md`
- Existing broad runtime filter: `build/Stride.Runtime.slnf`
- Build/processing targets: `sources/targets/Stride.targets`, `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`
- Candidate core/runtime projects:
  - `sources/core/Stride.Core*.csproj`
  - `sources/engine/Stride.csproj`, `Stride.Engine.csproj`, `Stride.Games.csproj`, `Stride.Input.csproj`, `Stride.Graphics.csproj`, `Stride.Rendering.csproj`
  - Optional/legacy candidates (`Stride.Audio`, `Stride.UI`, `Stride.Navigation`, `Stride.Particles`, `Stride.BepuPhysics`, `Stride.Physics`, `Stride.Video`, `Stride.Voxels`, `Stride.SpriteStudio.Runtime`, `Stride.Assets`, `Stride.Shaders.Compiler`, `Stride.Shaders.Parser`)

### Scripts used for enumeration
- One ad-hoc Python script (run via stdin, not committed) to extract:
  - `TargetFramework/TargetFrameworks`
  - `StrideRuntime`, `StrideExplicitWindowsRuntime`, `StrideAssemblyProcessor`, `StrideAssemblyProcessorOptions`, `StrideGraphicsApiDependent`, `UseWPF`, `UseWindowsForms`
  - direct `ProjectReference` and `PackageReference`

### No-modification confirmation
- This audit pass is read-only except creating this report file.
- No project/source/build file was modified.

### Static-only uncertainty
- True Linux buildability cannot be proven without `dotnet restore/build` on proposed slice.
- Actual runtime viability (window creation, graphics init, content loading) cannot be validated statically.
- Some dependency behavior is conditionally defined via SDK props/targets; static inference is conservative.

---

## 2) Strategic framing (Stri‑V Core doctrine)

This audit adopts the hardfork doctrine as build-governance:
- **Stri‑V Core is the supported runtime surface.**
- **Legacy Stride editor/tooling systems must not block core extraction.**
- Legacy systems may remain in-repo, but are excluded from M1 core slice.
- **M1a is a minimal compilable foundation, not a full engine.**
- Legacy source asset compiler is out-of-scope for M1a unless hard-required.
- Runtime serialization/content primitives are allowed if core needs them.
- YAML source-asset editing and Quantum editor graph workflows are excluded unless proven runtime-critical.

---

## 3) Candidate project universe

## 4) Project dependency graph

(Combined for compactness; include/exclude decisions reflect M1a-first strategy.)

| Layer | Project | Key direct refs | Key flags/findings | Linux feasibility | M1 status |
|---|---|---|---|---|---|
| L0 | `sources/core/Stride.Core/Stride.Core.csproj` | none | `StrideRuntime=true`, `StrideAssemblyProcessor=true`, AP opts `--auto-module-initializer --serialization` | **Medium** (AP dependency) | Include |
| L0 | `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj` | `Stride.Core` | runtime + AP | **Medium** | Include |
| L0 | `sources/core/Stride.Core.IO/Stride.Core.IO.csproj` | `Stride.Core` | runtime + AP; `SharpDX` pkg | **Medium/Unknown** (SharpDX packaging behavior on Linux unknown) | Include |
| L0 | `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj` | `Stride.Core` | runtime + AP | **Medium** | Include |
| L0 | `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj` | `Stride.Core.IO`, `Stride.Core.MicroThreading` | runtime + AP, serializer infrastructure | **Medium** | Include |
| L0+ | `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj` | `Stride.Core.Serialization` | runtime; no explicit AP flag in project | **High/Medium** | Include (if needed by downstream) |
| L1 | `sources/engine/Stride/Stride.csproj` | `Stride.Core.Serialization`, `Stride.Core.Mathematics`, `tools/Stride.FreeImage` | runtime + AP; pulls FreeImage | **Possible blocker** (native/tool coupling) | Defer to M1b+ |
| L1 | `sources/engine/Stride.Engine/Stride.Engine.csproj` | `Stride.Audio`, `Stride.Rendering`, `Stride.VirtualReality`, `Stride.Shaders.Compiler` | runtime + AP; broad pull incl shader compiler | **Low/Unknown** for minimal Linux-first | Defer |
| L2 | `sources/engine/Stride.Games/Stride.Games.csproj` | `Stride.Graphics` | `StrideExplicitWindowsRuntime=true`, `StrideGraphicsApiDependent=true`, conditional `UseWPF/UseWindowsForms` | **Possible blocker** | Defer |
| L2 | `sources/engine/Stride.Input/Stride.Input.csproj` | `Stride.Games` | explicit windows runtime + graphics API dep + Win input packages | **Expected blocker risk** | Exclude from M1a |
| L3 | `sources/engine/Stride.Graphics/Stride.Graphics.csproj` | `Stride.Core.Tasks`, `Stride.Shaders`, `Stride` | graphics-api-dependent; packages include Vulkan+SDL but also D3D packages | **Unknown/Possible blocker** | Defer to M1d |
| L3 | `sources/engine/Stride.Rendering/Stride.Rendering.csproj` | `Stride.Games` | runtime + AP | **Coupled to L2 blockers** | Defer |
| L4 | `Stride.BepuPhysics` | `Stride.Engine` | AP opts serialization+parameter-key | **Unknown** (inherits L1 blockers) | Defer to M1e |
| L4 | `Stride.UI`, `Stride.Audio`, `Stride.Navigation`, `Stride.Particles` | mostly `Stride.Engine` (or `Stride.Physics`) | optional gameplay/runtime systems | **Unknown** | Defer |
| Legacy | `Stride.Physics` (old), `Stride.Video`, `Stride.Voxels`, `Stride.SpriteStudio.Runtime` | `Stride.Engine` (+windows/d3d in video) | outside M1 doctrine | **N/A** | Exclude |
| Legacy tooling | `sources/assets/*`, `sources/editor/*`, `sources/presentation/*` | editor/buildgraph heavy | quantum/yaml/editor/build tooling | **N/A** | Exclude |

### Proposed M1a dependency table (direct+transitive within proposed slice)
- `Stride.Core`
- `Stride.Core.Mathematics -> Stride.Core`
- `Stride.Core.IO -> Stride.Core`
- `Stride.Core.MicroThreading -> Stride.Core`
- `Stride.Core.Serialization -> Stride.Core.IO + Stride.Core.MicroThreading (+ Stride.Core transitively)`
- `Stride.Core.Reflection -> Stride.Core.Serialization`

No direct references in this proposed M1a set to:
- `sources/assets/*`
- `sources/editor/*`
- `sources/presentation/*`
- shader compiler/parser projects
- mobile projects
- games/input/graphics/rendering projects

---

## 5) AssemblyProcessor requirements for proposed M1a slice

| Project | AP enabled | AP options | Why likely needed | If disabled (theory) |
|---|---|---|---|---|
| Stride.Core | Yes | `--auto-module-initializer --serialization` | serializer registration + module init | likely missing generated serialization/registration paths |
| Stride.Core.Mathematics | Yes | `--auto-module-initializer --serialization` | math-type serializer generation/registration | runtime serialization gaps possible |
| Stride.Core.IO | Yes | `--auto-module-initializer` | module init behavior | registration/bootstrap may not run |
| Stride.Core.MicroThreading | Yes | `--auto-module-initializer` | module init hooks | init behavior risk |
| Stride.Core.Serialization | Yes | `--auto-module-initializer --serialization` | serializer framework glue | core serializer registry break risk |
| Stride.Core.Reflection | Not set explicitly in project file | (inherits defaults only if set externally) | may consume AP-generated metadata indirectly | unknown; likely safer with AP ecosystem intact |

AssemblyProcessor target wiring (`Stride.AssemblyProcessor.targets`) indicates AP is not cosmetic; it is integrated before output copy and can gate compilation pipeline.

---

## 6) Runtime content / asset dependency boundary

| Subsystem | M1a classification | Evidence-based rationale |
|---|---|---|
| `sources/assets/*` | Excluded from M1a | editor/source asset pipeline scope; not in proposed core csproj refs |
| YAML source asset serializer | Excluded from M1a | primarily source asset workflows; prior audit identified runtime content path as separate |
| `Package/PackageSession/AssetItem` | Excluded from M1a | package/editor build orchestration concerns |
| Quantum | Excluded from M1a | prior audit: editor/property-graph-centric |
| Legacy asset compiler | Excluded from M1a | hardfork doctrine and prior blockers |
| `ContentManager`/runtime serialization primitives | **Required for later runtime stages (M1b+)** | tied to runtime engine/content loading; M1a foundational compile can start beneath it |
| DataSerializer infrastructure | **Required for M1a core serialization layer** | in `Stride.Core.Serialization` and AP serialization options |
| Shader compiler/parser | Required later (M1d likely) | pulled by `Stride.Engine` (`Stride.Shaders.Compiler`) |
| Old source asset pipeline | Excluded from M1a | legacy dragon per doctrine |

---

## 7) Linux-specific feasibility audit (proposed path)

| Item | Classification | Notes |
|---|---|---|
| AssemblyProcessor task loading | **Expected blocker** | already flagged by prior canary audits; AP is build-time critical |
| `Stride.Games` explicit windows runtime / conditional WPF/WinForms | **Possible blocker** | project has windows-runtime markers; include only after M1a |
| `Stride.Input` windows packages (`SharpDX.DirectInput`, `XInput`, `MMI`) | **Possible blocker** | conditional by TFM but signals windows-oriented coupling |
| Graphics D3D package coupling in `Stride.Graphics` | **Possible blocker** | project includes D3D11/12 compiler packages even with Vulkan options present |
| Vulkan package presence (`Vortice.Vulkan`) | Not blocker by itself | favorable for later cross-platform target |
| SDL package presence (`Silk.NET.Sdl`) | Not blocker by itself | favorable for later platform spine |
| FreeImage/OpenAL/native libs | **Possible blocker** | appear in engine-level projects, not M1a core set |
| `net10.0-windows` / WindowsDesktop framework refs | Unknown in full graph | static evidence shows conditional WPF/WinForms usage; avoid those projects in M1a |
| Shader parser/compiler dependencies | **Possible blocker** for engine stage | not in M1a set |
| Path/shell assumptions | Unknown | AP target uses temp/hash copy and msbuild task load; platform-specific behavior possible |

---

## 8) Proposed staged extraction plan (.slnf plan only)

### Stage M1a — `build/StriV.Core.M1a.slnf` (foundational managed core)
- Include: `Stride.Core`, `Stride.Core.Mathematics`, `Stride.Core.IO`, `Stride.Core.MicroThreading`, `Stride.Core.Serialization`, optionally `Stride.Core.Reflection`.
- Exclude: all engine/editor/assets/presentation/tooling/mobile projects.
- First commands:
  - `dotnet restore build/StriV.Core.M1a.slnf`
  - `dotnet build build/StriV.Core.M1a.slnf -c Debug`
- Expected blockers: AssemblyProcessor task loading.
- Proves: minimal core graph feasibility baseline.
- Does not prove: engine runtime, window/input, graphics.

### Stage M1b — `build/StriV.Engine.M1b.slnf`
- Add minimal engine foundation (`Stride`, then `Stride.Engine` candidates as validated).
- Keep `Stride.Assets`, editor, presentation, game studio excluded.
- Proves: engine assembly-level composition beyond core.

### Stage M1c — `build/StriV.Platform.M1c.slnf`
- Add platform spine: `Stride.Games`, `Stride.Input` (only if Linux-safe path can be selected).
- Proves: window/input integration viability.

### Stage M1d — `build/StriV.Graphics.M1d.slnf`
- Add `Stride.Graphics`, `Stride.Rendering`, shader projects only as unavoidable.
- Bias to Vulkan/SDL path; keep D3D11 legacy out of acceptance criteria.
- Proves: render spine compiles in Linux-first discipline.

### Stage M1e — `build/StriV.Gameplay.M1e.slnf`
- Add optional gameplay modules (priority: `Stride.BepuPhysics`, then UI/audio/navigation/particles).
- Proves: post-core module layering.

---

## 9) Proposed first implementation target (smallest practical)

**Target choice:** create `build/StriV.Core.M1a.slnf` with foundational managed core only.

### Exact proposed project list
1. `sources/core/Stride.Core/Stride.Core.csproj`
2. `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
3. `sources/core/Stride.Core.IO/Stride.Core.IO.csproj`
4. `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
5. `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
6. `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj` (optional but recommended)

### Proposed commands (next task)
- `dotnet restore build/StriV.Core.M1a.slnf`
- `dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal`

### Expected result
- Most likely outcome: restore succeeds; build may fail at AssemblyProcessor task stage.

### Top 3 likely blockers
1. AssemblyProcessor task load/execution on Linux.
2. Hidden AP-generated serializer dependencies even in core projects.
3. Package/runtime assumptions from core dependencies (e.g., `SharpDX` in `Stride.Core.IO`).

### Should this run on Linux sandbox?
- **Yes.** It is the right first discipline target for Linux-first hardfork extraction.

---

## 10) Legacy exclusion list (M1 scope boundary)

| System | Representative paths | Why excluded | Handling | Risk if accidentally included |
|---|---|---|---|---|
| Game Studio/editor | `sources/editor/*` | out of M1 core | quarantine later; keep ref | massive graph expansion, WPF/tooling coupling |
| WPF/presentation | `sources/presentation/*` | editor UI stack, windows bias | quarantine later | linux build blockers |
| Quantum editor graph | `sources/presentation/Stride.Core.Quantum*`, `sources/assets/Stride.Core.Assets.Quantum*` | editor/source graph workflow | keep as reference | drags editor complexity |
| YAML source editing layer | `sources/core/Stride.Core.Yaml*`, `sources/assets/*` | source asset tooling, not minimal compile core | keep reference | unnecessary complexity in M1a |
| Legacy asset compiler | `sources/assets/*Compiler*`, buildengine-related projects | explicitly out of scope | quarantine | canary blocker surface area |
| Android/iOS | mobile-targeted projects under tests/samples/platform-specific TFMs | out of hardfork M1 | keep reference | multiplatform noise blocks Linux-first focus |
| Direct3D11 legacy | D3D packages in graphics/video projects | not required for M1a acceptance | defer | windows graphics coupling |
| Direct3D9/10 | if present, legacy | out of scope | keep reference | legacy API entanglement |
| Visual scripting | related assets/editor paths | out of M1 | quarantine | extra compiler/editor coupling |
| Old physics (Bullet) | `sources/engine/Stride.Physics*` | Bepu preferred later | defer | legacy physics complexity |
| Video | `sources/engine/Stride.Video` | optional, windows/native-heavy | exclude | FFmpeg/D3D/native blockers |
| Voxels | `sources/engine/Stride.Voxels` | optional/advanced module | defer | extra rendering deps |
| SpriteStudio | `sources/engine/Stride.SpriteStudio.*` | legacy optional module | defer | non-core content/runtime deps |
| Metrics | tooling/infra paths (if present) | non-core runtime | keep reference | distracts from core extraction |
| Launcher | launcher/tooling paths (if present) | non-core runtime | keep reference | packaging/runtime indirection |
| Full release packaging | packaging scripts/pipelines | not M1 compile target | defer | CI/release complexity |
| GPU gold-image matrix | graphics regression infrastructure | not M1a proof goal | defer | heavy infra requirements |

---

## 11) Recommended M1a implementation prompt (for next Codex task)

> Create `build/StriV.Core.M1a.slnf` containing only these projects: `Stride.Core`, `Stride.Core.Mathematics`, `Stride.Core.IO`, `Stride.Core.MicroThreading`, `Stride.Core.Serialization`, `Stride.Core.Reflection`. Then run exactly: `dotnet restore build/StriV.Core.M1a.slnf` and `dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal`. Do not edit any project/targets/source files, do not touch other solution filters, do not fix unrelated errors. Report restore/build output and blockers precisely (especially AssemblyProcessor failures) with file-path evidence.

---

## 12) Risk register

| Risk | Area | Likelihood | Impact | Evidence | Mitigation |
| ---- | ---- | ---------: | -----: | -------- | ---------- |
| AssemblyProcessor still blocks smallest core | Build pipeline | High | High | AP wired into opted-in core projects; prior canary blocker | Validate with M1a slnf first; isolate AP failure signatures |
| Core projects pull serialization/codegen unexpectedly | Core runtime | Medium | High | AP opts include serialization in core projects | Keep M1a minimal; avoid disabling AP; document required generated behaviors |
| Core graph accidentally pulls assets/compiler | Build graph | Medium | High | broader runtime filter already proved too broad | Start from hand-curated core-only slnf |
| `Stride.Games` forces windows runtime behavior | Platform layer | Medium | High | `StrideExplicitWindowsRuntime`, conditional WPF/WinForms | defer to M1c, keep out of M1a |
| Shader compiler/parser required earlier than expected | Graphics layer | Medium | Medium/High | `Stride.Engine` references `Stride.Shaders.Compiler` | keep engine out of M1a; stage at M1d |
| Native dependencies required earlier than expected | Runtime packaging | Medium | Medium | `Stride` references `Stride.FreeImage`; graphics includes native libs | delay `Stride`/graphics until staged introduction |
| Linux build behavior diverges from Windows | Cross-platform build | High | Medium/High | prior canary issues and AP task loading sensitivity | Linux-first incremental filter validation |
| `.slnf` omits required transitive project | Build orchestration | Medium | Medium | static-only assumptions can miss implicit requirements | adjust only by explicit build error evidence in next task |
