# Stri-V M0a: Current Repository State (Code-First Baseline)

Date: 2026-05-02 (UTC)
Scope: repository audit only; no deletion/refactor/migration/cleanup changes.

## Evidence collection commands

```bash
git rev-parse --show-toplevel
git branch --show-current
git log -1 --pretty=format:'%H%n%s'
git rev-list --count HEAD
git remote -v
find . -maxdepth 1 -mindepth 1 -printf '%P\n' | sort
rg --files -g '*.sln' -g '*.slnf'
rg --files -g '*.csproj' | wc -l
rg --files -g '*Directory.Build.props' -g '*Directory.Build.targets' -g 'Directory.Packages.props' -g 'global.json' -g 'nuget.config'
rg -n 'Android|iOS|Direct3D11|Direct3D12|Vulkan|Irony|Roslyn|VisualScript|BepuPhysics|MSDFGen' sources samples tests build docs .github
python - <<'PY'
import glob,collections
cs=glob.glob('**/*.csproj',recursive=True)
print(collections.Counter(p.split('/')[0] for p in cs))
print('sources',len([p for p in cs if p.startswith('sources/')]))
print('samples',len([p for p in cs if p.startswith('samples/')]))
print('build',len([p for p in cs if p.startswith('build/')]))
print('deps',len([p for p in cs if p.startswith('deps/')]))
print('test-like',len(glob.glob('**/*Tests*.csproj',recursive=True)))
PY
```

---

## 1) Baseline

- Repository name (inferred from folder): `stri-v`.
- Current branch: `work`.
- Latest commit: `3f155bf309cd7d0a64b681fcba491c952d157b27`.
- Latest commit message: `Update README.md: Hard fork`.
- Approximate number of commits: `4435`.
- Remotes: none listed by `git remote -v` at audit time.
- Solution files currently present:
  - `build/Stride.sln`
  - `build/Stride.VisualStudio.sln`
  - `build/Stride.Launcher.sln`
  - Multiple solution filters under `build/*.slnf` including platform/runtime/test slices (`Stride.Android.slnf`, `Stride.iOS.slnf`, `Stride.Runtime.slnf`, test-focused filters).
  - Additional solutions under `samples/`, `sources/metrics/`, `sources/tools/Stride.TextureConverter.Wrappers/`.
- Top-level directory/file structure:
  - `.github`, `build`, `deps`, `docs`, `samples`, `sources`, `tests`
  - root metadata/config: `README.md`, `LICENSE.md`, `CODE_OF_CONDUCT.md`, `BACKERS.md`, `THIRD PARTY.md`, `.editorconfig`, `global.json`, `nuget.config`.
- Obvious fork-identity/community/infrastructure files are present:
  - docs/community/legal: `README.md`, `LICENSE.md`, `CODE_OF_CONDUCT.md`, `BACKERS.md`, `THIRD PARTY.md`
  - CI/workflows: `.github/workflows/*`.

## 2) Major directory map

| Path | Apparent purpose (direct evidence + conservative inference) | Important subdirectories | Type | Hardfork status |
|---|---|---|---|---|
| `sources/` | Engine/runtime/editor/tool source projects (largest code area; 137 csproj). | `engine/`, `editor/`, `presentation/`, `shaders/`, `tools/`, `shared/`, `localization/`, `metrics/`, `sdk/` | runtime/editor/tooling/core | **Keep + investigate by subsystem** |
| `samples/` | End-user and template samples; includes tutorials and physics sample. | `Tutorials/`, `Templates/`, `Tests/`, `Physics/`, `Audio/`, `Input/`, `Particles/` | samples/canary candidates | **Keep (for canary selection), later prune** |
| `tests/` | Test docs/assets and test infra references (plus many test projects inside `sources/`). | `GPU-TESTING.md`, likely helper data | tests/infrastructure | **Keep + investigate** |
| `build/` | Solutions, solution filters, scripts, build docs, legacy and CI-facing build orchestration. | `Stride.sln`, `*.slnf`, `Stride.build`, `compile.bat`, `docs/` | build/tooling/infra | **Keep + investigate (high-risk for platform removal)** |
| `deps/` | Dependency-related projects and likely vendored/native dependency assets. | `Stride.GitVersioning/` | dependencies | **Investigate** |
| `docs/` | Documentation, including editor and asset-system docs. | `editor/`, `asset-system/`, `stri-v/` | docs | **Keep** |
| `.github/` | CI workflows, issue templates, automation metadata. | `workflows/`, `ISSUE_TEMPLATE/` | infrastructure/CI | **Keep + investigate** |

## 3) Solution and project inventory

### Solutions
- Core solutions and filters are centralized in `build/` (`Stride.sln` + multiple `.slnf` platform/runtime/test slices).
- Extra focused solutions exist for samples/tutorials/templates and specific tooling/metrics.

### Projects (grouped)
- Total `*.csproj`: **191**.
  - `sources/`: **137** (primary engine/editor/tool code)
  - `samples/`: **52**
  - `build/`: **1** (`build/tools/CompareGold/CompareGold.csproj`)
  - `deps/`: **1** (`deps/Stride.GitVersioning/Stride.GitVersioning.csproj`)

### Test projects
- Name-matched “test-like” projects: about **48** (`*Tests*.csproj` pattern), mostly in `sources/` and sample test harnesses.
- Dedicated test solution filters exist (`build/Stride.Tests.*.slnf`).

### Sample projects
- Extensive sample coverage under `samples/` across tutorials/templates/audio/input/particles/physics.

### Tool projects
- Visible examples:
  - `build/tools/CompareGold/CompareGold.csproj`
  - `sources/tools/Stride.TextureConverter.Wrappers/Stride.TextureConverter.Wrappers.sln`

### Editor/Game Studio projects
- Substantial editor surface under `sources/editor/*` and docs referencing GameStudio/editor view models.

### Platform-specific projects/filters
- Explicit mobile filters exist: `build/Stride.Android.slnf`, `build/Stride.iOS.slnf`.
- Platform-targeted workflows/scripts mention Windows, Android, iOS, Vulkan/D3D11/D3D12 permutations.

### Native projects/tools (first pass)
- CI and build docs reference native dependency build flows (e.g., FreeType workflows).
- No deep native project cataloging done in M0a (defer to follow-up audit).

### Package/version management
- `global.json`
- `nuget.config`
- `sources/Directory.Build.props`
- `sources/sdk/Directory.Build.props`
- `sources/Directory.Packages.props` (central package management present in `sources/` scope).

## 4) Current subsystem map (first pass)

| Subsystem | Representative paths | Apparent purpose | Hardfork status | Confidence | Notes / risks |
|---|---|---|---|---|---|
| Runtime core | `sources/engine/*` | Core runtime/engine libraries | Keep | Medium | Needs M0b dependency graph to isolate minimal Windows runtime. |
| Graphics | `tests/GPU-TESTING.md`, `.github/workflows/test-windows-game.yml`, `sources/engine/*Graphics*` | Multi-backend rendering/test orchestration | Keep Vulkan + D3D12; investigate D3D11 removal path | High | Active CI/testing includes D3D11, D3D12, Vulkan. |
| Asset pipeline | `sources/assets*`, `sources/editor/Stride.Assets.*`, `docs/asset-system/*` | Asset definitions, compilers, registration and editor integration | Keep + investigate | Medium | Potential coupling with editor and scripting. |
| Editor/Game Studio | `sources/editor/*`, `docs/editor/*`, localization entries | WPF/GameStudio authoring environment | Investigate then selective keep/remove | High | Contains Roslyn editor scripting and VisualScript surfaces targeted for removal. |
| UI | `sources/*UI*`, sample/template UI-related assets | Runtime/editor UI | Keep + investigate | Medium | Need split between runtime UI and editor-only UI. |
| Physics | `sources/engine/Stride.BepuPhysics/*`, `samples/Physics/BepuSample/*` | Physics integration and tests; Bepu present | Replace/standardize on Bepu; investigate legacy physics remnants | High | “Bullet” hits in quick scan appear mostly sample content names, not proof of Bullet backend status. |
| Audio | `samples/Audio/*`, likely `sources/engine/*Audio*` | Runtime audio and audio samples | Keep + investigate | Medium | Verify platform/native backend dependencies. |
| Serialization/content | `sources/*Serialization*`, asset/content systems | Data formats/content runtime | Keep | Low | Not deeply enumerated in M0a. |
| Scripting | `sources/editor/Stride.Assets.Presentation/...ScriptSourceFileAssetViewModel.cs` | Script editing/compilation authoring flows | Investigate; remove Roslyn editor scripting per hardfork direction | High | RoslynPad/Roslyn workspace references confirmed. |
| Visual scripting | `docs/editor/editors.md`, `sources/editor/.../VisualScriptEditor/*` (via doc/localization refs) | Node/graph-based scripting editor | Remove (target), after dependency audit | High | Likely intertwined with editor UX and asset formats. |
| Tests | `build/Stride.Tests.*.slnf`, `sources/*Tests*.csproj`, `samples/Tests/*` | Unit/integration/GPU/sample testing | Keep + curate canary | High | Existing structure useful for runtime canary gating. |
| Samples | `samples/*` | Feature demos/templates/tutorials | Keep subset for canary | High | Some samples mobile-centric; suitability must be filtered. |
| Native/external tooling | `.github/workflows/dep-freetype.yml`, `sources/tools/*`, build scripts | External deps and native tool wrappers | Investigate/reduce | Medium | Hardfork wants fewer native executables/tooling. |
| Build/CI infrastructure | `.github/workflows/*`, `build/*.build`, `build/*.bat`, solution filters | Build matrix and automation | Keep + aggressively simplify | High | Mobile and multi-platform matrix currently broad. |

## 5) Immediate hardfork-relevant observations (first pass)

- **Android**: strong presence in workflows, build scripts, solution filters, docs.
  - Examples: `.github/workflows/build-android.yml`, `build/Stride.Android.slnf`, `build/Stride.build` targets.
- **iOS**: strong presence in workflows, build scripts, solution filters, docs.
  - Examples: `.github/workflows/build-ios.yml`, `build/Stride.iOS.slnf`, `build/Stride.build` targets.
- **Direct3D 9 / 10**: no obvious direct string matches in first-pass grep; unknown if represented via abstractions/legacy identifiers.
- **Direct3D 11**: explicit active references in Windows runtime/test workflows.
- **Direct3D 12**: explicit active references in Windows runtime/test workflows and GPU testing docs.
- **Vulkan**: explicit active references in workflows and GPU testing docs.
- **Bullet**: no clear engine-side Bullet backend evidence in quick string scan; “Bullet” appears in sample asset naming and gameplay variable names. Requires deeper targeted audit.
- **BepuPhysics**: clearly present in solution and dedicated physics projects/tests; sample exists (`samples/Physics/BepuSample`).
- **Roslyn**: explicit editor references (RoslynPad integration and Roslyn workspace usage).
- **Visual scripting**: explicit editor/documentation presence (`VisualScriptEditor*` paths in docs and localization references).
- **Irony**: explicit shader parser project included in solutions (`sources/shaders/Irony/Irony.csproj` and `build/Stride*.slnf/.sln`).
- **MSDFGen.exe**: no direct first-pass string hit for exact token `MSDFGen.exe`; unknown whether equivalent tooling exists under other names.
- **Native binaries/native tools**: native dependency build workflows (e.g., FreeType) are present; deeper catalog pending.

## 6) Build and CI first-pass notes

- CI workflow files are present under `.github/workflows/` and include platform-specific runtime builds/tests (Windows/Android/iOS) and dependency builds.
- Build scripts/orchestration present:
  - `build/Stride.build`
  - `build/compile.bat`
  - `build/Stride.AllPlatforms.bat`
  - platform-related project files in `build/`.
- Version/package config present:
  - `global.json`
  - `nuget.config`
  - `sources/Directory.Build.props`
  - `sources/sdk/Directory.Build.props`
  - `sources/Directory.Packages.props`.
- Obvious custom infrastructure exists (solution filters by platform/test profile + custom `.build` orchestration).

## 7) Candidate canary surfaces (preliminary)

1. `build/Stride.Runtime.slnf`
   - Exercises: runtime-focused build graph without full editor stack.
   - Assumptions: multi-platform entries may still exist; can likely constrain to Windows + chosen backend.
   - Suitability: high as initial compile canary, but needs backend/platform narrowing.

2. `build/Stride.Tests.Game.slnf` / `build/Stride.Tests.Game.GPU.slnf`
   - Exercises: runtime game tests, potentially graphics output paths.
   - Assumptions: may require GPU/backend setup; CI currently includes D3D11/12/Vulkan permutations.
   - Suitability: high for rendering/runtime regressions; medium operational complexity.

3. `samples/Tests/Stride.Samples.Tests.csproj`
   - Exercises: sample-level behavior; likely broad API surface.
   - Assumptions: some tests may include mobile/platform expectations.
   - Suitability: medium-high; useful after trimming platform matrix.

4. `samples/Physics/BepuSample/BepuSample.sln`
   - Exercises: Bepu-focused runtime path.
   - Assumptions: graphics backend and desktop runtime availability.
   - Suitability: high for “Bullet→Bepu” migration confidence.

5. `samples/Templates/FirstPersonShooter/FirstPersonShooter.sln` (or similar template sample sln)
   - Exercises: representative game template runtime + asset pipeline.
   - Assumptions: may depend on editor-generated content structure.
   - Suitability: medium as user-facing canary.

## 8) Unknowns and follow-up audits

### Build graph unknowns
- Exact minimal project set for Windows-only runtime under .NET 8+.
- Whether mobile projects are transitively referenced by core solutions/scripts.

### Graphics/backend unknowns
- Where D3D11 is required today vs optional.
- Whether latent D3D9/D3D10 support still exists under non-obvious identifiers.
- Backend abstraction boundaries for safe D3D11 removal.

### Editor/tooling unknowns
- Roslyn editor scripting dependency graph and data formats affected by removal.
- Visual scripting project/asset/runtime coupling depth.

### Asset pipeline unknowns
- Asset compiler/runtime compatibility if visual scripting and Roslyn scripting are removed.
- Irony dependency scope (shader compiler only vs broader).

### Physics unknowns
- Legacy physics backend status and exact coexistence model with Bepu.
- Test/sample coverage adequacy for Bepu-only future.

### Native dependency unknowns
- Complete list of native binaries/tools consumed at build time/runtime.
- Whether MSDFGen functionality exists via renamed binaries/scripts.

### Test/canary unknowns
- Fastest deterministic canary on Windows CI and local dev.
- Gold-image/GPU test stability strategy when backend matrix is reduced.

## 9) Recommended next M0 reports

1. **M0b – Build graph and solution dependency audit (Windows runtime minimal set).**
2. **M0c – Graphics backend audit (D3D11/D3D12/Vulkan + latent D3D9/10 detection).**
3. **M0d – Editor scripting & visual scripting removal impact audit (Roslyn + VisualScript).**
4. **M0e – Physics backend audit (legacy physics vs Bepu, migration risk map).**
5. **M0f – Native/tooling dependency audit (executables, wrappers, CI-produced native artifacts incl. MSDF-related tooling).**

---

### Evidence vs inference policy used in this report
- “Present/explicit” claims are based on direct file/path/string evidence from commands listed above.
- “Apparent purpose” and “hardfork status” are conservative inferences from naming/structure and were marked as such.
- No deletion/refactor recommendations are made without follow-up audit.
