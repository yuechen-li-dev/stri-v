# 420 — Clean Project Graph Plan for Stri-V Core (M3a Audit/Design)

## 1) Evidence collection

### Commands used

```bash
find /workspace -name AGENTS.md -print
rg -n "<ProjectReference|<PackageReference|Reference Include|HintPath|TargetFramework|TargetFrameworks|OutputType|RootNamespace|AssemblyName|AllowUnsafeBlocks|Nullable|ImplicitUsings|DefineConstants|InternalsVisibleTo|AssemblyInfo|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideRuntime|StrideGraphicsApiDependent|StrideIncludeShaderCompiler|StrideIncludeAudio|StrideIncludeVirtualReality|Compile Include|Compile Remove|None Include|Content Include|EmbeddedResource|Analyzer|PackageVersion" sources samples build Directory.Build.props Directory.Build.targets sources/Directory.Packages.props
rg -n "using Stride\.|namespace Stride|InternalsVisibleTo|DataContract|DataMember|ContentSerializer|ModuleInitializer|ParameterKey|EffectCompiler|AudioSystem|VirtualReality|BepuPhysics" sources samples/StriV/CoreSmoke
find sources/core sources/engine/Stride sources/engine/Stride.Games sources/engine/Stride.Graphics sources/engine/Stride.Input sources/engine/Stride.Engine sources/engine/Stride.BepuPhysics samples/StriV/CoreSmoke -maxdepth 4 -type f | sort
rg -n "ProjectReference|PackageReference|TargetFramework|StrideAssemblyProcessor|DefineConstants|AssemblyName|RootNamespace|OutputType|Compile Include|Compile Remove|None Include|Content Include|StrideGraphicsApiDependent|StrideIncludeShaderCompiler|StrideIncludeAudio|StrideIncludeVirtualReality|AllowUnsafeBlocks|Nullable|ImplicitUsings" <14 target csproj files>
sed -n '1,220p' <14 target csproj files>
```

### Files opened/read (primary)

- `sources/Directory.Packages.props`
- `build/docs/SDK-GUIDE.md`
- The 14 migration target `.csproj` files listed in this audit prompt
- `sources/core/Stride.Core/build/Stride.Core.targets`
- `build/striv-build-core-m1a.sh`
- `build/striv-build-coresmoke-m1g.ps1`

### Scripts used

- No custom script authored in this task.
- Existing shell commands only.

### Confirmation: file-modification scope

- This audit intentionally modified only this report file.
- No source moves, no legacy project deletion, no refactors.

### Static-only uncertainty

- AP (AssemblyProcessor) requirement details for each assembly are inferred from current `StrideAssemblyProcessorOptions` and source patterns; exact minimal set still requires iterative build validation.
- Graphics/API file gating granularity (Vulkan-only/D3D-only separability) is partly inferential and requires compile trials with exclusion sets.

---

## 2) Current pain recap

The inherited Stride graph is too entangled for efficient Stri-V local development:

- Custom SDK props/targets drive behavior not obvious from per-project `.csproj` files.
- Visual Studio design-time view diverges from CLI behavior.
- Packaging/payload items pollute runtime project item graphs (`build/*`, `deps/AssemblyProcessor/*`, analyzer payload copy behavior).
- Graphics API inner-build machinery (`StrideGraphicsApiDependent`) introduces implicit target expansion and output-path complexity.
- D3D defaults and conditional package paths remain present in projects even when Linux/Vulkan is the intended lane.
- AssemblyProcessor tooling is tied to legacy packaging and hashed copy logic under `Stride.Core.targets`.
- Legacy editor/asset/tooling hooks remain mixed into runtime projects through references and constants.

---

## 3) New build doctrine

1. Prefer plain `Microsoft.NET.Sdk` projects.
2. Use explicit `ProjectReference` edges; no hidden SDK graph magic.
3. Use explicit `PackageReference` only when required.
4. No legacy Stride SDK imports in the clean graph.
5. Keep AP source-built and invoked explicitly (target/script owned by Stri-V).
6. No checked-in AP binary payload dependency.
7. No asset/editor/compiler hook by default in core graph.
8. Shader compiler/audio/VR are opt-out by default in M3 clean spine.
9. Keep old project files as read-only reference, not authoritative build path.
10. Defer all physical source moves.

---

## 4) Project naming and namespace policy

Recommendation:

- **Layout:** Option A variant: `striv/StriV.Core.slnx` + `striv/projects/<ProjectName>/<ProjectName>.csproj`.
- **Assembly names / root namespaces:** Keep current `Stride.*` for engine/core libs to avoid churn.
- **Sample:** Keep `StriV.CoreSmoke` name/namespace.
- **No namespace migration in M3.**

Rationale: this cleanly decouples build-graph artifacts from legacy `sources/*` project files while preserving assembly identity and source paths.

---

## 5) Source include strategy

General pattern for new clean projects:

- `EnableDefaultCompileItems=false`
- Include `Compile` globs from existing source folder(s), e.g. `../../../../sources/core/Stride.Core/**/*.cs`
- Explicitly include shared file link:
  - `sources/shared/SharedAssemblyInfo.cs` as linked `Properties/SharedAssemblyInfo.cs`
- Exclude by default from all clean projects:
  - `**/bin/**`, `**/obj/**`, packaging metadata files
  - legacy `build/*.props|*.targets` pack payload items
  - legacy deps payload copy items
- Keep existing project-local `Properties/AssemblyInfo.cs` files compiled where present.
- AP project remains multi-target (`netstandard2.0;net10.0`) initially, unless later simplified by proving net10-only safe.

Subsystem-specific exclusions in clean runtime profile:

- `Stride.Engine`: preserve current conditional removals for shader compiler/audio/VR and make them explicit by default constants.
- `Stride.Graphics`: initially include all sources but set Vulkan/Linux constants and omit D3D package refs in first clean profile where possible; if compile breaks, introduce explicit source excludes.

---

## 6) Project-by-project migration table

| Old project | New clean project path | AssemblyName | RootNamespace | TargetFramework | Source includes | Source excludes | Project refs | Package refs | DefineConstants | Unsafe? | AssemblyProcessor? | Notes/Risks |
| ----------- | ---------------------- | ------------ | ------------- | --------------- | --------------- | --------------- | ------------ | ------------ | --------------- | ------- | ------------------ | ----------- |
| `sources/core/Stride.Core/Stride.Core.csproj` | `striv/projects/Stride.Core/Stride.Core.csproj` | `Stride.Core` | `Stride.Core` | `net10.0` | `sources/core/Stride.Core/**/*.cs` + shared info | packaging payload entries, legacy build items | none | `ServiceWire`, `System.ValueTuple`, `Microsoft.NETCore.Platforms` | Linux/Desktop baseline | yes | yes (`--auto-module-initializer --serialization`) | Remove pack/deps behaviors from clean graph |
| `Stride.Core.Mathematics` | `striv/projects/Stride.Core.Mathematics/...` | `Stride.Core.Mathematics` | `Stride.Core.Mathematics` | `net10.0` | folder glob + shared info | n/a | `Stride.Core` | none | baseline | yes | yes (`--auto-module-initializer --serialization`) | |
| `Stride.Core.IO` | `striv/projects/Stride.Core.IO/...` | `Stride.Core.IO` | `Stride.Core.IO` | `net10.0` | folder glob + shared info | android-specific zip remove rules can likely be dropped for Linux-only | `Stride.Core` | none (drop UWP-only `SharpDX`) | baseline | yes | yes (`--auto-module-initializer`) | validate zip-related conditional sources |
| `Stride.Core.MicroThreading` | `striv/projects/Stride.Core.MicroThreading/...` | same | same | `net10.0` | folder glob + shared info | n/a | `Stride.Core` | none | baseline | yes | yes (`--auto-module-initializer`) | |
| `Stride.Core.Serialization` | `striv/projects/Stride.Core.Serialization/...` | same | same | `net10.0` | folder glob + shared info | n/a | `Stride.Core.MicroThreading`,`Stride.Core.IO` | `K4os.Compression.LZ4.Legacy` | baseline | yes | yes (`--auto-module-initializer --serialization`) | |
| `Stride.Core.Reflection` | `striv/projects/Stride.Core.Reflection/...` | same | same | `net10.0` | folder glob + shared info | n/a | `Stride.Core.Serialization` | none | baseline | no | likely no | confirm AP need via build |
| `Stride` | `striv/projects/Stride/Stride.csproj` | `Stride` | `Stride` | `net10.0` | folder glob + shared info | remove tool ref if avoidable | `Stride.Core.Serialization`,`Stride.Core.Mathematics` | `System.Drawing.Common` (if still used on Linux), maybe drop if not needed | baseline | yes | yes (`--auto-module-initializer --serialization`) | currently references `Stride.FreeImage`; likely keep first pass |
| `Stride.Games` | `striv/projects/Stride.Games/...` | same | `Stride` (current) | `net10.0` | folder glob + shared info | winforms/wpf platform GUI extras | `Stride.Graphics` | none | `STRIDE_UI_SDL` + baseline | yes | yes (`--auto-module-initializer`) | remove `StrideGraphicsApiDependent` behavior |
| `Stride.Graphics` | `striv/projects/Stride.Graphics/...` | same | `Stride` (current) | `net10.0` | folder glob + shared info | possibly D3D-only files in phase 2 | `Stride.Core.Tasks`,`Stride.Shaders`,`Stride` (or narrowed minimal set if extracted) | `Vortice.Vulkan`,`Silk.NET.Sdl`,`MSDF-Sharp.Core`,`Remora.MSDFGen`,`System.Memory` | `STRIDE_GRAPHICS_API_VULKAN` + baseline | yes | yes | highest risk area due to API gating |
| `Stride.Input` | `striv/projects/Stride.Input/...` | same | `Stride` (current) | `net10.0` | folder glob + shared info | win-only input backend files/packages | `Stride.Games` | none for Linux first | `STRIDE_UI_SDL` + baseline | yes | yes (default/no options) | remove DirectInput/XInput/MMI refs initially |
| `Stride.Engine` | `striv/projects/Stride.Engine/...` | same | `Stride` | `net10.0` | folder glob + shared info | shader/audio/VR files (same as current conditional removes) | `Stride.Input`,`Stride.Rendering` (+ optional excluded refs omitted) | `System.ValueTuple`,`System.Threading.Tasks.Dataflow` | + `STRIDE_ENGINE_WITHOUT_*` flags | yes | yes (`--parameter-key --auto-module-initializer --serialization`) | preserve current opt-out behavior explicitly |
| `Stride.BepuPhysics` | `striv/projects/Stride.BepuPhysics/...` | same | `Stride.BepuPhysics` | `net10.0` | folder glob | n/a | `Stride.Engine` | `BepuPhysics` | baseline | no | yes (`--serialization --parameter-key`) | confirm render/debug dependencies |
| `StriV.CoreSmoke` | `striv/projects/StriV.CoreSmoke/...` | `StriV.CoreSmoke` | `StriV.CoreSmoke` | `net10.0` | sample folder | n/a | `Stride.Engine` | none | baseline runtime flags via props | no | no | exe smoke app |
| `Stride.Core.AssemblyProcessor` | `striv/projects/Stride.Core.AssemblyProcessor/...` | same | `Stride.Core.AssemblyProcessor` | `netstandard2.0;net10.0` | AP folder + explicit linked core files currently in csproj | remove legacy copy-to-deps targets in clean graph | none | `Mono.Cecil`,`Mono.Options`,`Microsoft.Build.Framework`,`Microsoft.Build.Utilities.Core`,`Microsoft.NET.StringTools`,`Polysharp` | `STRIDE_ASSEMBLY_PROCESSOR;STRIDE_PLATFORM_DESKTOP` | yes | n/a (tool itself) | keep as bootstrap tool |

---

## 7) Package reference audit

### Likely required in clean core graph

- Core libs: `ServiceWire`, `System.ValueTuple`, `Microsoft.NETCore.Platforms`, `K4os.Compression.LZ4.Legacy`.
- Engine/graphics/input runtime spine: `Vortice.Vulkan`, `Silk.NET.Sdl`, `System.Threading.Tasks.Dataflow`, `System.Memory`, `MSDF-Sharp.Core`, `Remora.MSDFGen`.
- Physics: `BepuPhysics`.
- AP tool: `Mono.Cecil`, `Mono.Options`, `Microsoft.Build.Framework`, `Microsoft.Build.Utilities.Core`, `Microsoft.NET.StringTools`, `Polysharp`.

### Likely to exclude initially (clean Linux/Vulkan-first)

- D3D packages (`Silk.NET.Direct3D11`, `Silk.NET.Direct3D12`, `Silk.NET.Direct3D.Compilers`, SharpDX family).
- Win-only input packages (`SharpDX.DirectInput`, `SharpDX.XInput`, `Microsoft.Management.Infrastructure`).
- VR packages (`Silk.NET.OpenXR*`) unless explicitly needed.
- Shader compiler/editor/assets NuGet layers.
- Audio/native legacy payload-oriented packages.

Unknown: exact need for `System.Drawing.Common` in `Stride` for Linux headless path (keep for first pass, prune after validation).

---

## 8) Define constants / feature flags audit

Recommended explicit default constants for clean M3 core profile:

- `STRIDE_PLATFORM_LINUX`
- `STRIDE_PLATFORM_DESKTOP`
- `STRIDE_UI_SDL`
- `STRIDE_GRAPHICS_API_VULKAN`
- `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER`
- `STRIDE_ENGINE_WITHOUT_AUDIO`
- `STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY`

Constants to avoid in this profile:

- `STRIDE_UI_WINFORMS`
- `STRIDE_UI_WPF`
- `STRIDE_GRAPHICS_API_DIRECT3D11`
- mobile/UWP constants

Implementation recommendation: centralize in `striv/build/StriV.Core.Profile.props` imported by clean projects.

---

## 9) AssemblyProcessor strategy

### Which projects require AP (initial)

- Keep AP enabled where legacy project already explicitly enables it, preserving option strings.
- Minimum initial AP-ON set: Core libs, `Stride`, `Stride.Games`, `Stride.Graphics`, `Stride.Input`, `Stride.Engine`, `Stride.BepuPhysics`.
- AP-OFF candidate: `StriV.CoreSmoke`.

### Short-term (M3b first implementation)

- Build `Stride.Core.AssemblyProcessor` first from source.
- Invoke AP from a **new minimal Stri-V target** under `striv/build/StriV.AssemblyProcessor.targets` or from build script post-compile step.
- Pass explicit options per project (copied from current project options).
- Disable AP execution in design-time builds (`DesignTimeBuild=true` condition).

### Long-term

- Replace per-project option duplication with centralized AP profile mapping.
- Add incremental input hash for AP runs in `obj/` only.
- Evaluate shrinking AP scope if some projects compile/runtime-pass without processing.

### If AP not run

Likely breakages include missing serialization metadata, missing parameter key registrations, and module initializer side-effects.

---

## 10) Graphics/platform strategy

- **Initial clean profile:** Linux + Vulkan only.
- Keep Windows/DX profile as future separate build profile.
- Do not reintroduce legacy graphics inner-build targets.
- Prefer constants-based compilation first; add source excludes only when compile errors prove necessary.

Risk note: `Stride.Graphics`/`Stride.Games`/`Stride.Input` may still contain cross-API assumptions requiring targeted file exclusions once tested.

---

## 11) Excluded subsystem strategy

- Shader compiler / CppNet / SDSL: excluded via `STRIDE_ENGINE_WITHOUT_SHADER_COMPILER` and source removes already demonstrated in existing project.
- Audio / Celt / OpenAL: excluded via `STRIDE_ENGINE_WITHOUT_AUDIO` and source removes.
- VR / OpenVR/OpenXR: excluded via `STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY` and omitted refs/packages.
- Old Stride.Physics: omitted from clean core spine.
- Asset compiler / Quantum / Game Studio/editor: omitted entirely from clean core solution.
- Bepu companions beyond `Stride.BepuPhysics`: defer unless compile demands additional project edges.

---

## 12) `.slnx` plan

Recommendation:

- Create primary solution at `striv/StriV.Core.slnx`.
- Include only clean projects for the 14-target scope.
- Keep `build/StriV.Core.slnx`, legacy `build/Stride.sln`, and `.slnf` files as historical/legacy lanes.

Expected VS behavior improvement:

- Improved source visibility and predictable evaluation due to plain SDK projects and explicit references.

---

## 13) Build scripts plan

Proposed scripts (coexisting initially with M1 scripts):

- `striv/build/striv-build-ap.sh` (+ `.ps1`) — build AP tool first.
- `striv/build/striv-build-core.sh` (+ `.ps1`) — restore/build clean solution.
- `striv/build/striv-run-coresmoke.sh` (+ `.ps1`) — run sample.
- `striv/build/striv-smoke-xvfb.sh` (+ `.ps1`) — optional CI/headless runtime smoke.

Coexistence strategy:

- Keep existing M1 scripts unchanged during transition.
- Mark new scripts as preferred M3+ path for local dev and CI pilot lane.

---

## 14) Risk register

| Risk | Likelihood | Impact | Evidence | Mitigation |
| ---- | ---------: | -----: | -------- | ---------- |
| Missing source includes | High | High | New glob graph is manual | Start narrow; compile iteratively; add include map checklist |
| Missing package refs | High | High | Many refs currently conditional via SDK | Add explicit package matrix per project; resolve build errors one-by-one |
| Missing define constants | High | High | SDK currently injects many defines | Central props profile with explicit constants |
| AP not run | High | High | Existing projects enable AP broadly | Build AP first, run AP in clean target/script |
| AP options wrong | Medium | High | Options differ per project today | Mirror current options initially; later optimize |
| Serialization/module initializer gaps | Medium | High | AP-based features | Runtime smoke tests + targeted serialization checks |
| Vulkan/SDL native copy behavior missing | Medium | Medium | Legacy deps targets perform native setup | Add minimal explicit runtime copy steps in new scripts/targets |
| Bepu/render dependency surprises | Medium | Medium | Bepu feature integration depth unclear statically | Keep Bepu optional lane until validated |
| Hidden needed SDK behavior | High | High | Current system relies on SDK targets | Document each blocker and replicate minimally only when proven required |
| VS confusion with out-of-tree globs | Medium | Medium | New project path != source path | Keep project tree shallow; test VS source explorer behavior early |
| Duplicate types from overlapping globs | Medium | High | Many folders with similarly named generated files | Disable default compile items; explicit include/exclude globs |
| Missing `.tt` generated outputs | Medium | Medium | Some `.tt` artifacts referenced | Verify generated `.cs` presence; add generation step only if missing |
| Accidental assembly name drift | Low | High | New project authoring phase risk | Pin `AssemblyName` explicitly in every clean project |
| Namespace churn | Low | High | Migration pressure | Keep `Stride.*` namespaces unchanged in this phase |

---

## 15) Recommended M3b implementation prompt

```text
Implement M3b: first clean Stri-V core project graph.

Scope:
- Create new clean SDK-style project files and a new clean solution at striv/StriV.Core.slnx.
- Do not move source files.
- Do not delete or edit legacy Stride project files except read-only reference.
- Keep assembly names/root namespaces aligned with current Stride.* outputs.
- Keep StriV.CoreSmoke as sample executable.

Build behavior:
1) Build sources/core/Stride.Core.AssemblyProcessor as source tool first.
2) Wire clean projects to invoke the source-built AP via a minimal Stri-V-owned target/script (no checked-in deps/AssemblyProcessor payload dependency).
3) Use explicit constants for Linux desktop Vulkan + SDL and explicit opt-outs for shader compiler/audio/VR.
4) Use explicit ProjectReference and PackageReference entries only.
5) Attempt restore/build of clean solution.
6) If build succeeds, run StriV.CoreSmoke.
7) Stop at first blocker and report exact file/line/error with a minimal remediation proposal.

Out of scope:
- No namespace renaming to StriV.*.
- No source refactors.
- No editor/assets/compiler graph enablement.
- No reintroduction of legacy SDK/import magic unless strictly required and justified.
```
