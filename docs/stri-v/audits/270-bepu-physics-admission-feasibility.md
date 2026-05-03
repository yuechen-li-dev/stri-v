# 270 — Bepu Physics admission feasibility (M1f prep, static)

## 1) Evidence collection

### Commands run
- `rg -n "<ProjectReference|<PackageReference|TargetFramework|TargetFrameworks|StrideRuntime|StrideAssemblyProcessor|StrideAssemblyProcessorOptions|StrideGraphicsApiDependent|UseWPF|UseWindowsForms|Microsoft.WindowsDesktop.App|Bepu|BepuPhysics|Stride.Engine|Stride.Physics|Stride.Audio|Stride.VirtualReality|Stride.Shaders.Compiler|Stride.Rendering|Stride.Graphics|Stride.Input|Assets|Editor|Presentation|Quantum|Compiler|Shader|Audio|VirtualReality|VR|Physics|Collider|RigidBody|Simulation|Constraint|Character|Vehicle|Navigation|Debug|Gizmo|Rendering|DllImport|LibraryImport|Native" sources/engine/Stride.BepuPhysics sources/engine/Stride.Physics* sources/engine/Stride.Engine samples/Physics build docs/stri-v`
- `find sources/engine/Stride.BepuPhysics -maxdepth 5 -type f | sort`
- `rg -n "using Stride.Physics|using Stride.Audio|using Stride.VirtualReality|using Stride.Shaders.Compiler|using Stride.Rendering|using Stride.Graphics|using Stride.Assets|using Stride.Editor|using Stride.Presentation|DllImport|LibraryImport|Native|Gizmo|Debug" sources/engine/Stride.BepuPhysics`
- `nl -ba` on all Bepu csproj files + `Stride.Engine.csproj` + `build/StriV.Engine.M1e.slnf` + M1e build scripts
- `find samples/Physics/BepuSample -maxdepth 4 -type f | sort`
- `rg -n "TargetFramework|UseWPF|UseWindowsForms|Microsoft.WindowsDesktop.App|ProjectReference|PackageReference|Stride.Assets|Stride.Editor|Stride.Presentation|Stride.Rendering|Stride.Audio|Stride.VirtualReality|Stride.Physics|Stride.BepuPhysics|RuntimeIdentifier|StrideCompileAssets|StrideGraphicsApiDependent" samples/Physics/BepuSample`
- `rg -n "Stride\.Physics|Stride\.Audio|Stride\.VirtualReality|Stride\.Shaders\.Compiler|Stride\.Assets|Stride\.Editor|Stride\.Presentation|DllImport|LibraryImport" sources/engine/Stride.BepuPhysics/Stride.BepuPhysics`

### Files opened/read
- `sources/engine/Stride.BepuPhysics/**` (main + companion csproj and representative source files)
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `build/StriV.Engine.M1e.slnf`
- `build/striv-build-engine-m1e.sh`
- `build/striv-build-engine-m1e.ps1`
- `samples/Physics/BepuSample/**` (project metadata + asset coupling signals)

### Parsing/scripts
- No custom parser script was needed; static extraction was done with `rg`, `find`, and `nl -ba`.

### Integrity constraints confirmation
- No builds run.
- No tests run.
- No project/targets/scripts patched.
- Static-only uncertainty remains where compile-time symbol flow and transitive package behavior would require execution.

## 2) Current M1e baseline recap
- M1e `.slnf` explicitly includes core + `Stride`, `Stride.Games`, `Stride.Graphics`, `Stride.Input`, and `Stride.Engine`.
- Build flow bootstraps AssemblyProcessor from source first, then routes engine build with `StrideAssemblyProcessorFramework=net10.0`, `StrideAssemblyProcessorBasePath=<source-built output>`, `StrideAssemblyProcessorHash=sourcebuild`.
- Linux/Vulkan routing is explicit in scripts: `StridePlatforms=Linux`, `StrideGraphicsApis=Vulkan`.
- Opt-out switches used in M1e are explicit: `StrideIncludeShaderCompiler=false`, `StrideIncludeAudio=false`, `StrideIncludeVirtualReality=false`.
- M1e proves this reduced engine slice is buildable and usable as baseline.
- M1e does **not** prove Bepu compilation, optional Bepu companion modules, or sample/test asset ecosystems.

## 3) `Stride.BepuPhysics` project audit
Project: `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`

- Direct project references: only `..\..\Stride.Engine\Stride.Engine.csproj`.
- Package references: only `BepuPhysics`.
- Conditional package refs: none in this project file.
- Target framework behavior: no explicit `TargetFramework`/`TargetFrameworks`; inherited from shared SDK props.
- `StrideRuntime`: `true`.
- `StrideAssemblyProcessor`: `true`; options `--serialization --parameter-key`.
- `StrideGraphicsApiDependent`: not explicitly set in this project.
- WPF/WinForms/WindowsDesktop coupling: none declared.
- Direct dependency on `Stride.Engine`: yes.
- Direct dependency on old `Stride.Physics`: no evidence.
- Direct dependency on audio/VR/shader compiler/editor/assets/presentation: no direct project/package ref; has `InternalsVisibleTo Stride.Assets` only.
- Native dependency indicators (`DllImport`/`LibraryImport`): none found in main project sources.
- Linux feasibility (static): **medium-high**. Main risk is source-level dependency on rendering/gizmo APIs from engine.
- Recommendation: **include main `Stride.BepuPhysics` in M1f candidate; defer companion modules unless required**.

## 4) Bepu source dependency audit (main project)

### Simulation/world/system core
- Examples: `BepuSimulation.cs`, `PhysicsGameSystem.cs`, `BepuConfiguration.cs`, `SystemsOrderHelper.cs`.
- Purpose: integrate Bepu simulation lifecycle into Stride ECS/game systems.
- Implication: tight runtime integration with `Stride.Engine` game systems.
- Excluded-module dependency: no direct audio/VR/shader compiler evidence.
- Linux relevance: high.
- Risk: medium (engine integration assumptions).

### Body/static/collidable components
- Examples: `BodyComponent.cs`, `StaticComponent.cs`, `CollidableComponent.cs`, `CollidableStack.cs`.
- Purpose: entity components and runtime links to Bepu handles.
- Excluded-module dependency: none directly to audio/VR/shader compiler/editor.
- Linux relevance: high.
- Risk: low-medium.

### Collider definitions
- Examples: `Definitions/Colliders/*.cs`, including `MeshCollider.cs`.
- Purpose: collider schemas and conversions.
- Dependency implication: some colliders reference rendering/mesh types (`Stride.Rendering` usage in `MeshCollider.cs`).
- Excluded-module dependency: rendering yes; audio/VR/shader compiler no direct evidence.
- Linux relevance: high.
- Risk: medium.

### Constraints
- Examples: `Constraints/*.cs`.
- Purpose: physics constraints wrappers/components.
- Dependency implication: mostly managed Bepu + engine component patterns.
- Excluded-module dependency: none found.
- Linux relevance: high.
- Risk: low.

### Character/controller
- Example: `CharacterComponent.cs`.
- Purpose: character simulation integration.
- Excluded-module dependency: none found.
- Linux relevance: high.
- Risk: low-medium.

### Vehicle
- No obvious vehicle-specific files in main project tree.
- Status: unknown/not present as a distinct module.

### System/order/processor integration
- Examples: `Systems/CollidableProcessor.cs`, `Systems/ConstraintProcessor.cs`.
- Implication: engine entity processor integration.
- Excluded-module dependency: `Stride.Rendering` is used by processor/gizmo-adjacent code.
- Risk: medium.

### Rendering/debug/gizmo-related files
- Examples: `Systems/CollidableGizmo.cs`, `Systems/ConstraintGizmo.cs`, `Systems/ShapeCacheSystem.cs`.
- Purpose: editor/runtime gizmo rendering support and geometry cache.
- Dependency implication: uses `Stride.Engine.Gizmos`, `Stride.Graphics`, `Stride.Rendering`, materials APIs.
- Excluded-module dependency: depends on rendering stack (already in M1e), not audio/VR/shader compiler by direct evidence.
- Linux relevance: medium.
- Risk: medium-high (if gizmo APIs imply extra compile constraints).

### Design/editor/asset-related
- Evidence: only `InternalsVisibleTo("Stride.Assets")` in csproj; no direct editor namespace usage in main sources.
- Risk: low.

### Old physics bridge
- No `using Stride.Physics` in main project source.
- No project reference to `Stride.Physics`.
- Risk: low.

### Native interop
- No `DllImport`/`LibraryImport` hits in main project.
- Risk: low.

**Answers:**
- Main project requires old `Stride.Physics`? **No direct evidence.**
- Main project requires `Stride.Rendering` / gizmo types? **Yes, direct source usage exists.**
- Main project requires audio/VR/shader compiler? **No direct evidence.**
- Main project requires editor/assets? **No direct hard dependency found; only `InternalsVisibleTo Stride.Assets`.**
- Native interop markers? **None found in main project sources.**

## 5) Optional companion projects audit

- `Stride.BepuPhysics.Debug/Stride.BepuPhysics.Debug.csproj`
  - Purpose: debug rendering helpers.
  - Depends on engine + main Bepu + rendering shader assets.
  - M1f: **defer** (non-minimal, debug-feature surface).

- `Stride.BepuPhysics.Navigation/Stride.BepuPhysics.Navigation.csproj`
  - Purpose: navigation integration via DotRecast and `Stride.Navigation`.
  - Additional deps: `DotRecast.Recast.Toolset`, `Stride.Navigation`.
  - M1f: **defer** (adds non-core subsystem).

- `Stride.BepuPhysics.Soft/Stride.BepuPhysics.Soft.csproj`
  - Purpose: soft-body extension.
  - Additional deps: `BepuUtilities`, engine + main Bepu.
  - M1f: **defer** (optional extension).

- `Stride.BepuPhysics._2D/Stride.BepuPhysics._2D.csproj`
  - Purpose: 2D adaptation layer.
  - Deps: engine + main Bepu.
  - M1f: **defer** (optional dimension-specific layer).

- `Stride.BepuPhysics.Tests/Stride.BepuPhysics.Tests.csproj`
  - Purpose: test project, win-x64 runtime, graphics regression, packaged assets.
  - M1f: **defer** (test+assets+Windows coupling).

## 6) Old physics relationship
- Bepu main project references `Stride.Engine`, not `Stride.Physics`.
- Samples/tests may coexist conceptually with broader engine modules, but no static proof main Bepu requires old physics.
- Old `Stride.Physics` can remain excluded for M1f.
- Namespace/type collision risk: low in compile-slice if `Stride.Physics` remains absent.
- M1 policy recommendation: keep old physics excluded; only revisit on concrete compile/runtime blocker.

## 7) Bepu sample context (`samples/Physics/BepuSample`)
- WindowsDesktop/Windows target: yes (`net10.0-windows`, `win-x64` in sample projects).
- Asset/editor/content pipeline dependency: high (`Stride.Assets`, `Stride.Core.Assets`, asset packages and `.sdpkg`/scene assets).
- Optional-module breadth: high (Video, UI, Particles, Rendering-heavy graphs).
- Future canary value: good for broader runtime scenario validation.
- M1f suitability: **too broad** for minimal compile-slice admission check.

## 8) Candidate M1f slice options

### Option A (recommended)
- Projects: M1e list + `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`.
- Transitives: `BepuPhysics` package + existing engine transitives.
- Excluded systems remain unchanged (audio/VR/shader compiler opt-out, no old physics).
- Expected blockers: gizmo/rendering API coupling in main Bepu code paths.
- Proves: core Bepu admission viability on top of no-audio/no-VR/no-shader-compiler engine.
- Doesn’t prove: debug/navigation/soft/2D/tests/sample viability.

### Option B
- Adds Bepu Debug project.
- Likely blockers: debug rendering/shader asset paths.
- Proves more visualization surface, but violates smallest-slice principle.
- Recommendation: defer.

### Option C
- Adds Navigation/Soft/2D companions.
- Likely blockers: added dependencies (`Stride.Navigation`, DotRecast, soft-body extension specifics).
- Proves broader ecosystem, not minimal admission.
- Recommendation: defer.

### Option D
- Defer Bepu entirely.
- Current static evidence does **not** justify full defer.
- Recommendation: not preferred.

## 9) Proposed next implementation target
- Smallest credible target: **Option A**.
- Proposed filter: `build/StriV.Engine.Bepu.M1f.slnf`.
- Explicit projects to include:
  1. `..\sources\core\Stride.Core\Stride.Core.csproj`
  2. `..\sources\core\Stride.Core.Mathematics\Stride.Core.Mathematics.csproj`
  3. `..\sources\core\Stride.Core.IO\Stride.Core.IO.csproj`
  4. `..\sources\core\Stride.Core.MicroThreading\Stride.Core.MicroThreading.csproj`
  5. `..\sources\core\Stride.Core.Serialization\Stride.Core.Serialization.csproj`
  6. `..\sources\core\Stride.Core.Reflection\Stride.Core.Reflection.csproj`
  7. `..\sources\engine\Stride\Stride.csproj`
  8. `..\sources\engine\Stride.Games\Stride.Games.csproj`
  9. `..\sources\engine\Stride.Graphics\Stride.Graphics.csproj`
  10. `..\sources\engine\Stride.Input\Stride.Input.csproj`
  11. `..\sources\engine\Stride.Engine\Stride.Engine.csproj`
  12. `..\sources\engine\Stride.BepuPhysics\Stride.BepuPhysics\Stride.BepuPhysics.csproj`

- Script strategy: mirror existing M1e script pattern (new M1f-named wrappers are optional but practical).
- Command pattern (source-built AP route):
  - `dotnet build <AP project> -c <Debug|Release> -v minimal`
  - `dotnet build build/StriV.Engine.Bepu.M1f.slnf -c <Debug|Release> -v minimal -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:StrideIncludeShaderCompiler=false -p:StrideIncludeAudio=false -p:StrideIncludeVirtualReality=false -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath=<AP output dir with trailing slash> -p:StrideAssemblyProcessorHash=sourcebuild`
- Expected first blockers:
  - main-project gizmo/rendering code assumptions;
  - any inherited engine symbol gating mismatches under current opt-out constants.

## 10) Risk register

| Risk | Candidate/project | Likelihood | Impact | Evidence | Mitigation |
| ---- | ----------------- | ---------: | -----: | -------- | ---------- |
| Bepu depends on Engine opt-out paths | Main Bepu | Medium | Medium | Main Bepu compiles against `Stride.Engine` only | Validate via isolated M1f build first, no extra modules |
| Bepu requires rendering/gizmo types | Main Bepu | High | Medium | `CollidableGizmo`, `ConstraintGizmo`, `ShapeCacheSystem`, `MeshCollider` use rendering/gizmos | Keep M1e rendering projects in filter; log first compile misses |
| Bepu unexpectedly requires old physics | Main Bepu | Low | High | No `Stride.Physics` ref/using found | Keep `Stride.Physics` excluded; only add if concrete errors demand |
| Package/native dependency surprise | Main Bepu package | Medium | Medium | Uses NuGet `BepuPhysics`; no native interop markers in source | Restore/build and inspect resolved assets if failure occurs |
| Editor/assets design-time coupling | Main Bepu | Low | Medium | Only `InternalsVisibleTo Stride.Assets` observed | Avoid asset/editor projects in M1f; validate pure compile path |
| Companion modules pulled accidentally | M1f filter scope | Medium | Medium | Several optional Bepu projects exist nearby | Explicitly include only main Bepu csproj in `.slnf` |
| M1f filter misses transitive dependency | `.slnf` | Medium | Medium | Filter includes explicit project set only | Iterate by first blocker evidence, avoid broad adds |
| AssemblyProcessor routing still required | entire slice | High | High | M1e scripts enforce source-built AP routing | Reuse exact AP bootstrap/property pattern |

## 11) Recommended implementation prompt (for next Codex task)

Create `build/StriV.Engine.Bepu.M1f.slnf` containing the exact M1e project list plus only `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`. If needed, add M1f build wrapper script(s) by mirroring the current M1e AssemblyProcessor bootstrap pattern (build AP first; validate payload; pass `StrideAssemblyProcessorFramework=net10.0`, `StrideAssemblyProcessorBasePath=<...>/`, `StrideAssemblyProcessorHash=sourcebuild`). Then run restore/build for M1f only on Linux/Vulkan with `StrideIncludeShaderCompiler=false`, `StrideIncludeAudio=false`, `StrideIncludeVirtualReality=false`. Do not include `Stride.Physics` or Bepu companion modules (`Debug`, `Navigation`, `Soft`, `_2D`, `Tests`) unless a direct compile blocker proves necessity. Do not fix unrelated errors. Report first blockers with exact file/line diagnostics.
