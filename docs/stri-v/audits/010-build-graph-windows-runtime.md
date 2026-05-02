# Stri-V M0b: Build Graph + Windows Runtime Baseline Audit

Date: 2026-05-02 (UTC)
Scope: repository audit only, code-first mapping of build graph for Windows runtime baseline planning.

## 1) Evidence collection

### Shell commands used

```bash
git branch --show-current
git log -1 --pretty=format:'%H%n%s'
git status --short
rg --files -g '*.sln' -g '*.slnf'
rg --files -g '*.csproj' -g '*.vcxproj' -g '*.fsproj'
rg --files -g 'Directory.Build.props' -g 'Directory.Build.targets' -g 'Directory.Packages.props' -g 'global.json' -g 'nuget.config' -g '*.props' -g '*.targets' build sources .github
rg -n 'Android|iOS|Xamarin|net8|net[0-9]|TargetFramework|TargetFrameworks|RuntimeIdentifier|RuntimeIdentifiers|PlatformTarget|Direct3D11|Direct3D12|Vulkan|Windows|WinExe|UseWPF|UseWindowsForms|Avalonia|Stride.Runtime|Stride.Engine|Stride.Graphics' build sources samples tests .github
sed -n '1,220p' build/Stride.Runtime.slnf
sed -n '1,260p' build/Stride.Tests.Game.slnf
sed -n '1,260p' build/Stride.Tests.Game.GPU.slnf
sed -n '1,220p' build/Stride.Android.slnf
sed -n '1,220p' build/Stride.iOS.slnf
cat global.json
sed -n '1,220p' sources/Directory.Packages.props
sed -n '1,220p' sources/Directory.Build.props
sed -n '1,220p' sources/sdk/Directory.Build.props
sed -n '1,220p' .github/workflows/build-windows-runtime.yml
sed -n '1,200p' .github/workflows/test-windows-game.yml
```

### Script used for classification/enumeration

A one-off inline Python script was used to roughly bucket project paths by area and surface unknowns. This was a path-prefix heuristic only (not authoritative dependency analysis).

### Environment assumptions

- Repository root assumed at `/workspace/stri-v`.
- Bash shell on Linux host, using ripgrep and Python 3.
- No full solution builds/tests were run in this audit pass; this is build-graph/static-evidence mapping.

### Failed/skipped commands

- `rg --files -g 'AGENTS.md'` returned no matches (non-error for audit, but no local agent instruction file found).
- `rg -n ...` produced very large output; follow-up used targeted file reads to avoid over-claiming from truncated output.

---

## 2) Build entry points

| Path | Purpose | Scope | Hardfork action | Confidence |
|---|---|---|---|---|
| `build/Stride.sln` | Monolithic primary solution. | Engine + editor + tools + tests (inferred from naming + filters referencing it). | Investigate/simplify; not minimal canary. | Medium |
| `build/Stride.Runtime.slnf` | Runtime-focused filter. | Runtime engine stack without full editor/test surface. | Keep; strong compile canary candidate. | High |
| `build/Stride.Tests.Game.slnf` | API-independent game test subset. | Windows game test assemblies. | Keep; runtime canary layer. | High |
| `build/Stride.Tests.Game.GPU.slnf` | GPU/API-sensitive game tests. | D3D11/D3D12/Vulkan matrix coupling. | Keep; critical for backend transition safety. | High |
| `build/Stride.Android.slnf`, `build/Stride.iOS.slnf` | Mobile runtime slices. | Android/iOS runtime build graph. | Remove later; first map coupling/dependencies. | High |
| `build/Stride.build` + `build/*.bat` | Local orchestration/legacy entry scripts. | Cross-platform orchestration, all-platform convenience. | Investigate and reduce post-canary. | Medium |
| `.github/workflows/build-windows-runtime.yml` | CI Windows runtime build. | Runtime + selectable graphics API. | Keep and eventually narrow default API set. | High |
| `.github/workflows/test-windows-game.yml` | CI runtime+GPU tests. | Game tests + matrix over D3D11/12/Vulkan. | Keep; matrix likely load-bearing. | High |
| `.github/workflows/build-android.yml`, `build-ios.yml` | Mobile CI builds. | Mobile workloads and platform-specific props. | Removal candidates after dependency proof. | High |
| `build/docs/SDK-GUIDE.md` | Build SDK/props behavior docs. | MSBuild SDK composition. | Keep; useful for safe simplification. | Medium |

---

## 3) Solution and solution-filter classification

Observed `.sln/.slnf` count: 25.

- **Full engine/editor solution** (approx 3)
  - `build/Stride.sln`, `build/Stride.VisualStudio.sln`, `build/Stride.Launcher.sln`
  - Relevance: not minimal; useful reference graph.
- **Runtime-focused** (1)
  - `build/Stride.Runtime.slnf`
  - Strong candidate for compile canary.
- **Test-focused** (6)
  - `build/Stride.Tests.Simple.slnf`, `build/Stride.Tests.Game.slnf`, `build/Stride.Tests.Game.GPU.slnf`, linux variants, `Stride.Tests.VSPackage.slnf`
  - Key to maintaining regression coverage during removals.
- **Platform-specific** (2)
  - `build/Stride.Android.slnf`, `build/Stride.iOS.slnf`
  - Direct mobile removal surface.
- **Sample/template/tutorial** (9)
  - `samples/StrideSamples.sln`, `samples/Tests/Tests.sln`, `samples/Templates/*/*.sln`, `samples/Tutorials/*/*.sln`
- **Tooling/dependency** (4)
  - `build/Stride.AssemblyProcessor.sln`, `sources/metrics/Stride.Metrics.sln`, `sources/tools/Stride.TextureConverter.Wrappers/Stride.TextureConverter.Wrappers.sln`, `samples/Physics/BepuSample/BepuSample.sln`
- **Unknown**
  - none strong; most paths classifiable by folder/purpose.

Blessed compile canary candidates: `build/Stride.Runtime.slnf` (primary), `build/Stride.Tests.Game.slnf` (secondary).

---

## 4) Project inventory by area (counts are approximate/path-heuristic)

- Runtime core: ~20 (e.g., `sources/core/Stride.Core/*.csproj`) → **keep**.
- Graphics: ~13 (e.g., `sources/engine/Stride.Graphics/`, `Stride.Rendering`, `sources/shaders/`) → **keep/investigate backend coupling**.
- Editor/Game Studio: ~8 (`sources/editor/*`) → **simplify/investigate** (Roslyn/visual scripting removal targets).
- Asset/content pipeline: ~21 (`sources/assets/*`, texture converter/importer tools) → **keep/simplify**.
- UI: ~14 (`sources/engine/Stride.UI*`, `sources/presentation/*`) → **investigate runtime-vs-editor split**.
- Physics: ~10 (`sources/engine/Stride.Physics*`, `sources/engine/Stride.BepuPhysics/*`) → **replace/standardize on Bepu, investigate coexistence**.
- Audio: ~4 (`sources/engine/Stride.Audio*`) → **keep**.
- Input: ~4 (`sources/engine/Stride.Input*`) → **keep**.
- Tools: ~17 (`sources/tools/*`, `build/tools/*`, launcher) → **simplify**.
- Tests: concentrated throughout `sources/*Tests*` + `samples/Tests` + `tests` docs/data → **keep curated canaries**.
- Samples/templates/tutorials: ~52 projects under `samples/` → **keep selected canaries; prune later**.
- Platform-specific: explicit Android/iOS/WIndows test variants present → **mobile remove later after coupling audit**.
- Native/dependency: vcxproj under `sources/native/*`, DirectXTex wrappers, deps project(s) → **investigate load-bearing status**.

Target framework quick pattern (sampled via grep): heavy `net10.0`, `net10.0-windows`, plus SDK/shared properties for editor/xplat targets.

---

## 5) Windows runtime minimal-set investigation

### Candidate A: `build/Stride.Runtime.slnf`
- Included projects: 27 (from JSON list in filter).
- Main included: core runtime libs + engine runtime modules + `sources/shaders/Irony` + `Stride.Core.Shaders` + `Stride.FreeImage`.
- Exclusions: editor/test projects excluded.
- Editor included? **No obvious editor projects**.
- Android/iOS included? **No platform-specific test/mobile projects in filter list**.
- D3D11/12/Vulkan coupling: indirect via `StrideGraphicsApis` properties in build workflows.
- Compile canary suitability: **High**.
- Runtime canary suitability: **Low/Medium** alone (build-only confidence).
- Risks: includes Irony and native-adjacent tooling dependencies.

### Candidate B: `build/Stride.Tests.Game.slnf`
- Included projects: 6.
- Projects: Assets.Tests + Audio/Input/Navigation/Particles + Engine.NoAssets Windows tests.
- Editor included? No.
- Android/iOS included? No (Windows-only test project variants here).
- Backend coupling: built in CI with D3D11 for “common” lane.
- Compile canary: Medium.
- Runtime canary: High for non-GPU test surface.
- Risks: may still rely on runtime asset/tool chain assumptions.

### Candidate C: `build/Stride.Tests.Game.GPU.slnf`
- Included projects: 6.
- Projects include `Stride.Graphics.Tests`, `Stride.Graphics.Tests.10_0`, `Stride.Graphics.Tests.11_0`, `Stride.Engine.Tests`, `Stride.Physics.Tests`, `Stride.UI.Tests` (Windows variants).
- Editor included? No.
- Android/iOS included? No in this filter.
- Backend coupling: explicit matrix (D3D11/D3D12/Vulkan) in CI.
- Compile canary: Medium.
- Runtime canary: Very High for rendering/backend regressions.
- Risks: gold-image and software renderer setup complexity (SwiftShader/WARP).

### Candidate D/E: `build/Stride.Android.slnf` and `build/Stride.iOS.slnf`
- Each includes ~25 runtime projects.
- Appears to reuse runtime surface with mobile platform property selection.
- Compile/runtime canary for Windows-first hardfork? **No**.
- Risk role: indicates mobile removal is a graph-first operation, not just docs cleanup.

---

## 6) Mobile platform build coupling

### Android
- Solution filter: `build/Stride.Android.slnf`.
- Projects: many `*.Android.csproj` test variants (e.g., `Stride.Engine.Tests.Android.csproj`, `Stride.UI.Tests.Android.csproj`, etc.).
- MSBuild refs: `StridePlatforms=Android`, `_AndroidNdkDirectory` in workflow.
- CI refs: `.github/workflows/build-android.yml`, `release.yml`, `main.yml`, dependency workflows.
- Tools/build scripts refs: `build/Stride.Android.TestApks.proj`, connection/test tools mention Android device flows.
- TFM/RID signals: Android workload install and Android-specific build props.
- Samples/tests/docs refs: `samples/Tests/*` platform constants and mobile sample mentions.
- Classification: mostly **build graph removal candidates**, but test/tool entries are **potentially load-bearing**.

### iOS
- Solution filter: `build/Stride.iOS.slnf`.
- Projects: `*.iOS.csproj` test variants and iOS-targeting assets/tests.
- MSBuild refs: `StridePlatforms=iOS` in workflow.
- CI refs: `.github/workflows/build-ios.yml`, `release.yml`, `main.yml`, dep-freetype iOS jobs.
- Tools/build scripts refs: connection router has iOS tracker and relay tooling.
- TFM/RID signals: iOS workload install in CI.
- Samples/tests/docs refs: sample tests and tool comments/reference code.
- Classification: **build graph removal candidates** plus **potentially load-bearing** tool/test hooks.

Conclusion: Android/iOS are strong build-graph-first removal candidates, but require targeted M0c dependency audit to avoid breaking shared test/tooling flows.

---

## 7) Graphics backend build coupling

- **D3D11**
  - CI defaults to D3D11 in `build-windows-runtime.yml` and common game tests.
  - Package graph includes SharpDX.Direct3D11 + Silk.NET.Direct3D11.
  - GPU test docs and thresholds mention D3D11 gold paths.
  - Status: **investigate before removal** (currently load-bearing in CI defaults).

- **D3D12**
  - Present in Windows test matrix + package versions.
  - Status: **keep** (hardfork target backend).

- **Vulkan**
  - Present in Windows + Linux GPU testing, SwiftShader dependencies/workflow setup.
  - Status: **keep** (hardfork target backend).

- **D3D9/D3D10**
  - No strong direct build-graph references surfaced in sampled files; `Stride.Graphics.Tests.10_0` exists and may represent feature-level/API compatibility tests (not proof of D3D10 backend).
  - Status: **unknown/investigate**.

Guidance from build graph only: D3D11 should not be touched early until CI canary strategy is updated and equivalent D3D12/Vulkan confidence exists.

---

## 8) Target frameworks and package management

- `global.json` SDK: `.NET SDK 10.0.100`, `rollForward: latestMinor`.
- Central package management: `sources/Directory.Packages.props` with `ManagePackageVersionsCentrally=true`.
- TFM patterns observed: `net10.0`, `net10.0-windows`, plus shared properties (`StrideEditorTargetFramework`, etc.) and SDK projects using `netstandard2.0`.
- WPF/WinForms signals:
  - `UseWindowsForms=true` in `sources/tools/Stride.ConnectionRouter/Stride.ConnectionRouter.csproj`.
  - WPF and Avalonia package versions present centrally.
- Mobile TFM/workload usage: explicit Android/iOS build workflows and mobile-specific project variants.
- Package signals:
  - Roslyn: multiple `Microsoft.CodeAnalysis.*`, RoslynPad packages.
  - Irony: dedicated `sources/shaders/Irony/Irony.csproj` in runtime filter.
  - BepuPhysics: `BepuPhysics` package version + dedicated engine projects.
  - Bullet: not explicitly surfaced in package management evidence.
  - Native tooling: `Remora.MSDFGen`, `MSDF-Sharp.Core`, FreeImage/DirectXTex/native vcxproj traces.
  - Graphics backends explicit packages: SharpDX D3D11/D3D12, Silk.NET Direct3D11/12, Vortice.Vulkan.

---

## 9) Build risk notes

- Removing Android/iOS may break:
  - workflow wiring (`main.yml`, `release.yml`, mobile build workflows),
  - tool flows (ConnectionRouter/TestRunner Android/iOS logic),
  - test project inclusion patterns.
- Removing D3D11 early likely breaks default Windows build/test lanes and possibly gold-image assumptions.
- GPU tests appear tightly coupled to backend matrix + software renderer provisioning.
- Runtime/editor coupling risk persists via shared SDK/props and assets/tool projects.
- Native dependency workflows (freetype/spirv/swiftshader, DirectXTex wrappers) may be required for current build completeness.
- Central package updates affect broad graph; dependency surgery should be staged after canary lock-in.

---

## 10) Recommended canary plan

### Layer 1: compile/build canary
- Candidate: `build/Stride.Runtime.slnf`.
- Proposed command (from CI evidence):
  - `dotnet build build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D11` (exact extra flags can follow CI profile once validated locally).
- Why minimal: excludes editor and most test/sample graph while retaining core runtime modules.
- Proves: runtime compile integrity and immediate build-graph health for Windows runtime surface.
- Does not prove: runtime behavior, rendering correctness, GPU-specific output, sample/template functionality.

### Layer 2: runtime/test canary
- Candidate: `build/Stride.Tests.Game.slnf` (common), plus `build/Stride.Tests.Game.GPU.slnf` for backend-sensitive lane.
- Proposed command (must be validated in environment):
  - `dotnet test build/Stride.Tests.Game.slnf` and backend-specific GPU test invocation aligned with CI.
- Why useful: catches runtime regressions and backend matrix regressions before destructive platform/backend deletions.
- Proves: executable runtime test behavior and graphics backend stability (with GPU lane).
- Does not prove: full editor workflows or mobile runtime behavior.

Rule preserved: no major deletion/chainsaw work until both layers are stable on hardfork CI baseline.

---

## 11) M0c recommendation

### Focus
A dependency-coupling audit for **mobile removal readiness** and **D3D11 decoupling readiness** in build/test infrastructure.

### Why
M0b shows mobile and D3D11 are still wired into CI defaults, solution filters, and several tool/test pathways.

### First files/projects for M0c
1. `.github/workflows/main.yml`, `build-windows-runtime.yml`, `test-windows-game.yml`, `build-android.yml`, `build-ios.yml`, `release.yml`.
2. `build/Stride.build`, `build/Stride.AllPlatforms.bat`, `build/Stride.Android.TestApks.proj`.
3. `sources/targets/Stride.targets`, `sources/sdk/Stride.Build.Sdk/Sdk/Stride.*.props/targets`.
4. Android/iOS test project variants under `sources/engine/*Tests/*.Android.csproj` and `*.iOS.csproj`.
5. Graphics test assets/docs: `tests/GPU-TESTING.md`, `tests/Stride.Engine.Tests/thresholds.jsonc`.
6. Tooling with mobile hooks: `sources/tools/Stride.ConnectionRouter/*`, `sources/tools/Stride.TestRunner/*`.

### Decisions still blocked
- Safe order and scope for Android/iOS graph removal.
- Whether D3D11 can be removed from early CI canaries without losing confidence.
- Which native tooling/dependency jobs can be removed without runtime/test fallout.
