# 160 — BepuPhysics Slice Feasibility (Static Audit)

## 1) Evidence collection

### Commands run
- `pwd && rg --files -g 'AGENTS.md'`
- `find .. -name AGENTS.md -print`
- `git status --short`
- `find sources/engine/Stride.BepuPhysics -maxdepth 4 -type f | sort`
- `find samples/Physics -maxdepth 4 -type f | sort`
- `rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|Microsoft.WindowsDesktop.App|Bepu|BepuPhysics|Stride.Engine|Stride.Physics|Physics|Collider|RigidBody|Simulation|Constraint|Character|Vehicle|Navigation|Rendering|Graphics|Audio|Assets|Editor|Presentation|Quantum|Compiler|Shader|Android|iOS|Windows|Linux|Vulkan|Direct3D|Native|DllImport" sources/engine/Stride.BepuPhysics sources/engine/Stride.Physics* sources/engine/Stride.Engine samples/Physics sources/sdk sources/targets`
- `rg -n "Stride.BepuPhysics|BepuPhysics|Stride.Physics" build sources samples tests -g "*.sln" -g "*.slnf" -g "*.csproj" -g "*.cs" -g "*.md"`
- `sed -n '1,220p' sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`
- `sed -n '1,220p' sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sed -n '1,220p' sources/engine/Stride.BepuPhysics/Stride.BepuPhysics.Debug/Stride.BepuPhysics.Debug.csproj`
- `sed -n '1,220p' samples/Physics/BepuSample/BepuSample.Game/BepuSample.Game.csproj`
- `sed -n '1,220p' sources/sdk/Stride.Build.Sdk/Sdk/Stride.Frameworks.props`
- `sed -n '1,260p' sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.props`
- `rg -n "using Stride.Engine|using Stride.Rendering|using Stride.Audio|using Stride.Assets|using Stride.Editor|using Stride.Presentation|using Stride.Physics|DllImport|LibraryImport|Native|Stride.VirtualReality" sources/engine/Stride.BepuPhysics`

### Search terms used
Project/package references, Stride runtime/build properties, platform/API routing, Windows desktop UI markers, physics API surface, engine coupling, and native interop terms.

### Files inspected (representative)
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Frameworks.props`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.props`
- `samples/Physics/BepuSample/BepuSample.Game/BepuSample.Game.csproj`
- `sources/engine/Stride.BepuPhysics/**` (inventory + targeted content searches)

### Parsing scripts
No custom parser script was used; analysis used direct `rg/find/sed` evidence.

### File modification confirmation
Static audit intent preserved during evidence collection; no project/target/source files were modified as part of analysis commands.

### Static-only uncertainty
No restore/build/test was executed, so compile-time breakpoints and runtime behavior remain unvalidated.

---

## 2) Current M1d baseline recap
- M1d explicit projects (as provided): six M1a core projects + `Stride`, `Stride.Games`, `Stride.Graphics`, `Stride.Input` plus transitive `Stride.FreeImage` and `Stride.Shaders`.
- M1d uses source-built AssemblyProcessor routing via bootstrap scripts (provided context).
- Linux/Vulkan routing aligns with SDK defaults (`StridePlatform=Linux` => default `StrideGraphicsApis=Vulkan`).
- M1d proves the input-era stack compiles in Linux Debug/Release with source-built AP.
- M1d does **not** prove that physics integration requiring `Stride.Engine` and its heavy transitive graph is already admitted.

---

## 3) `Stride.BepuPhysics` project audit

Target: `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`

- **Direct project references**: `..\..\Stride.Engine\Stride.Engine.csproj` (direct hard dependency).
- **Package references**: `BepuPhysics`.
- **Conditional package refs**: none found in this project file.
- **Target framework behavior**: no explicit TFM in project; uses Stride SDK runtime expansion (`StrideRuntime=true`) against SDK framework defaults.
- **`StrideRuntime`**: `true`.
- **`StrideAssemblyProcessor`**: `true` with `--serialization --parameter-key` options.
- **`StrideGraphicsApiDependent`**: not explicitly set in this project file.
- **WPF/WinForms/WindowsDesktop**: no `UseWPF`, `UseWindowsForms`, or `Microsoft.WindowsDesktop.App` markers in the Bepu main csproj.
- **Direct dependency on `Stride.Engine`**: yes (project reference).
- **Direct dependency on old `Stride.Physics`**: none in csproj and no `using Stride.Physics` in Bepu sources.
- **Direct dependency on rendering/audio/assets/editor/etc**:
  - Indirect via `Stride.Engine` (which references `Stride.Audio`, `Stride.Rendering`, `Stride.VirtualReality`, `Stride.Shaders.Compiler`).
  - In-source usage includes `Stride.Rendering` in Bepu systems and debug files.
- **Native dependency indicators**:
  - Main Bepu project only adds NuGet `BepuPhysics` package and no P/Invoke markers found in Bepu source search.
  - Native risk remains **unknown** without package restore/build artifact inspection.

**Linux feasibility (static):** **Medium-Low** as a *minimal* next slice, because Bepu main project fundamentally pulls `Stride.Engine`, and engine itself drags rendering/audio/VR/shader compiler.

**Recommendation:** Do **not** add only `Stride.BepuPhysics` on top of M1d as an isolated minimal slice; handle a minimal `Stride.Engine` slice first, then layer Bepu.

---

## 4) Bepu source audit

### A) Core simulation/world/runtime
- Representative files: `BepuSimulation.cs`, `PhysicsGameSystem.cs`, `BodyComponent.cs`, `StaticComponent.cs`, `CollidableComponent.cs`.
- Purpose: runtime simulation ownership and ECS component/system integration.
- Dependency implications: pervasive `Stride.Engine` usage; not a pure standalone physics-core wrapper.
- Linux relevance: high runtime relevance.
- Risk: engine-coupled APIs likely fundamental for current project layout.

### B) Collider/constraint/component surface
- Representative files: `Definitions/Colliders/*`, `Constraints/*`, `CharacterComponent.cs`.
- Purpose: collider descriptions and constraints exposed as Stride components.
- Dependency implications: heavy entity/component and design-attribute integration (`Stride.Engine`, `Stride.Engine.Design`).
- Linux relevance: high.
- Risk: broad API surface means deeper engine dependency.

### C) Selection/order/processing glue
- Representative files: `Definitions/ISimulationSelector.cs`, `SceneBasedSimulationSelector.cs`, `SystemsOrderHelper.cs`, processors.
- Purpose: scene/system orchestration.
- Dependency implications: depends on scene/system processor pipeline (`Stride.Engine.Processors`, flexible processing).
- Linux relevance: high.
- Risk: fundamental, not incidental.

### D) Rendering/debug draw adjacent code
- Representative files: `Systems/CollidableGizmo.cs`, `Systems/ConstraintGizmo.cs`, `Systems/ShapeCacheSystem.cs`, `Stride.BepuPhysics.Debug/*`.
- Purpose: gizmos and debug rendering.
- Dependency implications: direct `Stride.Rendering` coupling.
- Linux relevance: optional for headless physics but currently in module tree.
- Risk: raises rendering transitive pressure if not split.

### E) Navigation/soft/2D companion modules (separate projects)
- Representative dirs: `Stride.BepuPhysics.Navigation`, `.Soft`, `._2D`, `.Debug`, `.Tests`.
- Purpose: extra features and tests.
- Dependency implications: add broader systems and sample/test scope.
- Linux relevance: not needed for minimal M1e physics admission.
- Risk: scope creep if accidentally included.

### F) Native interop markers
- Search for `DllImport`/`LibraryImport` in Bepu tree: none found.
- Interpretation: mostly managed code signals, but package-level native assets still unknown statically.

**Answers:**
- Mostly managed/runtime code? **Yes**, based on source pattern.
- Assumes `Stride.Engine` APIs? **Yes, extensively.**
- Depends on old `Stride.Physics` abstractions? **No direct evidence.**
- Relies on editor/asset compiler systems? **Not in main runtime project; those appear in samples/tests/editor integration outside core csproj.**
- Native dependencies? **Unknown at package artifact level; no direct P/Invoke evidence in tree.**

---

## 5) Old physics relationship
- `Stride.BepuPhysics` appears as a separate module from `Stride.Physics` in solution/project layout.
- No direct Bepu->`Stride.Physics` reference found in `Stride.BepuPhysics.csproj` or Bepu source `using` scan.
- Samples are split: `BepuSample` uses `Stride.BepuPhysics`; `PhysicsSample` uses `Stride.Physics`.
- Therefore old physics can remain excluded from M1e unless future compile evidence disproves this.

**Policy recommendation for M1:** keep `Stride.Physics` excluded by default; only admit if a concrete compile blocker in chosen M1e slice requires it.

---

## 6) `Stride.Engine` coupling analysis
- Coupling is **direct in csproj** and **broad in source API use** (`Entity`, processors, design attributes, gizmo/rendering hooks).
- `Stride.Engine` itself directly references `Stride.Audio`, `Stride.Rendering`, `Stride.VirtualReality`, `Stride.Shaders.Compiler`.
- This indicates the current Bepu project is an engine integration layer, not a standalone low-level package.

Conclusions:
- `Stride.Engine` is required before/with current `Stride.BepuPhysics`.
- Coupling appears **fundamental for current structure**, not merely incidental.
- A future split (`Stride.BepuPhysics.Core` vs `Stride.BepuPhysics.Engine`) is plausible but would be refactor work.
- M1e should likely be deferred until a minimal `Stride.Engine` slice decision is made.

---

## 7) Sample/test context
- `samples/Physics/BepuSample/BepuSample.Game.csproj` targets `net10.0-windows` and references multiple broad packages (`Stride`, `Stride.Engine`, `Stride.Video`, `Stride.Particles`, `Stride.UI`, assets/compiler app, Bepu).
- Sample includes asset-heavy content and Windows app project (`BepuSample.Windows`).
- Suitable as future feature canary, but too broad for minimal Linux compile validation.

---

## 8) Candidate M1e slice options

### Option A — M1d + `Stride.BepuPhysics` only
- Projects: M1d set + `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`.
- Transitives: forces `Stride.Engine` via direct ProjectReference.
- Excluded systems: cannot effectively exclude engine transitive stack.
- Expected blockers: engine transitive breadth.
- Proves: little beyond implicit engine admission.
- Does not prove: isolated physics layer feasibility.
- **Recommendation: Not recommended.**

### Option B — M1d + `Stride.BepuPhysics` + `Stride.Engine`
- Projects: Option A plus explicit `Stride.Engine`.
- Transitives: audio/rendering/VR/shader compiler and their deps.
- Excluded systems: old physics still excluded.
- Expected blockers: large graph expansion.
- Proves: Bepu integrated with real engine stack.
- Does not prove: minimality.
- **Recommendation: Technically aligned with current structure, but larger than desired next step.**

### Option C — Defer Bepu; first minimal `Stride.Engine` slice
- Projects: M1d + `Stride.Engine` only (plus required transitive).
- Transitives: same heavy engine set, but isolates one variable (engine admission) before Bepu.
- Excluded systems: old physics/editor/assets remain excluded at slice definition level.
- Expected blockers: engine-level issues surfaced clearly.
- Proves: whether engine layer is viable on Linux source-build AP routing.
- Does not prove: Bepu-specific compile yet.
- **Recommendation: Best next incremental step.**

### Option D — Refactor split (`Core` + `Engine`) then add core first
- Projects: new refactored project(s) required.
- Transitives: potentially much smaller if successful.
- Excluded systems: can keep rendering/audio/VR out of core.
- Expected blockers: refactor scope and time.
- Proves: clean architecture target.
- Does not prove: immediate feasibility without code changes.
- **Recommendation: Long-term improvement, not prep-phase next action.**

---

## 9) Proposed next implementation target

**Smallest credible next target:** **Option C** (engine-first).

- Proposed `.slnf` name: `build/StriV.M1e.EngineSlice.slnf`.
- Explicit include list: M1d explicit projects + `sources/engine/Stride.Engine/Stride.Engine.csproj`.
- Bootstrap script: mirror existing M1d AP-routing bootstrap pattern (new script only if existing script is M1d-specific and cannot be parameterized).
- Restore/build command pattern (for implementation phase, not run now):

```bash
dotnet restore <M1e-slnf-or-sln> \
  -p:StridePlatforms=Linux \
  -p:StrideGraphicsApis=Vulkan \
  -p:StrideAssemblyProcessorFramework=net10.0 \
  -p:StrideAssemblyProcessorBasePath=<path-to-sourcebuilt-ap> \
  -p:StrideAssemblyProcessorHash=sourcebuild

dotnet build <M1e-slnf-or-sln> -c Debug \
  -p:StridePlatforms=Linux \
  -p:StrideGraphicsApis=Vulkan \
  -p:StrideAssemblyProcessorFramework=net10.0 \
  -p:StrideAssemblyProcessorBasePath=<path-to-sourcebuilt-ap> \
  -p:StrideAssemblyProcessorHash=sourcebuild
```

- Expected first blockers: engine transitive admission issues (audio/rendering/VR/shader compiler toolchain and associated platform assumptions).

If engine slice validates, then M1f can be `+ Stride.BepuPhysics` (main project only), still excluding old `Stride.Physics` unless compile evidence requires otherwise.

---

## 10) Risk register

| Risk | Candidate/project | Likelihood | Impact | Evidence | Mitigation |
| ---- | ----------------- | ---------: | -----: | -------- | ---------- |
| `Stride.BepuPhysics` requires `Stride.Engine` | `Stride.BepuPhysics.csproj` | High | High | Direct ProjectReference | Sequence engine-first slice |
| `Stride.Engine` pulls rendering/audio/VR/shader compiler early | `Stride.Engine.csproj` | High | High | Direct ProjectReferences | Isolate engine slice before Bepu |
| Bepu depends on old `Stride.Physics` | `Stride.BepuPhysics` | Low | Med | No direct ref/using found | Keep excluded; validate with compile later |
| Bepu native dependency surprise | `BepuPhysics` package | Unknown | Med | No P/Invoke in source; package artifacts not inspected | Validate on first restore/build in impl phase |
| Editor/assets leak into physics | samples/tests/editor | Med | Med | Broad references in sample/editor | Keep M1e scope to runtime projects only |
| Runtime component APIs unavailable without engine | Bepu source | High | High | Extensive `Stride.Engine` usage | Admit engine first |
| Sample project too broad | `BepuSample` | High | Med | `net10.0-windows` + many packages/assets | Exclude from M1e compile gate |
| AP routing still required | all runtime slices | High | High | Existing M1 pattern + `StrideAssemblyProcessor=true` | Reuse source-built AP bootstrap |
| `.slnf` misses needed explicit project | M1e filter | Med | Med | Historical risk in sliced builds | Start from known M1d list + add one layer only |

---

## 11) Recommended implementation prompt

> Create `build/StriV.M1e.EngineSlice.slnf` as the next incremental slice by starting from the validated M1d explicit project list and adding only `sources/engine/Stride.Engine/Stride.Engine.csproj`. Reuse (or minimally clone) the existing source-built AssemblyProcessor bootstrap routing pattern used by M1d. Run restore/build only for this M1e slice with Linux/Vulkan and source-built AP properties: `StridePlatforms=Linux`, `StrideGraphicsApis=Vulkan`, `StrideAssemblyProcessorFramework=net10.0`, `StrideAssemblyProcessorBasePath=<...>`, `StrideAssemblyProcessorHash=sourcebuild`. Do not include `Stride.Physics`, editor/assets/presentation/mobile, or unrelated projects. Do not fix unrelated errors; stop at first blockers and report them precisely with project/target/property context.

---

## Direct answers to core questions
1. Can `Stride.BepuPhysics.csproj` be added as next slice on Linux? **Not as an isolated minimal increment; it directly requires `Stride.Engine`.**
2. Does it depend on M1d graph or pull `Stride.Engine`? **It pulls `Stride.Engine` directly.**
3. If pulls engine, is coupling avoidable/fundamental? **Currently fundamental in this project shape.**
4. Does Bepu force old physics/rendering/assets/editor/audio/shader compiler? **Old physics: no direct force. Engine transitive set does pull rendering/audio/VR/shader compiler.**
5. Bepu dependencies managed or native? **Mainly managed by static evidence; native package-level details unknown until restore/build.**
6. Exact `.slnf` + build command if feasible? **Recommend engine-first `.slnf` (`StriV.M1e.EngineSlice.slnf`) with AP/Linux/Vulkan property pattern above; then add Bepu in next slice.**
7. If not feasible yet, smallest prerequisite? **Minimal `Stride.Engine` slice first.**
