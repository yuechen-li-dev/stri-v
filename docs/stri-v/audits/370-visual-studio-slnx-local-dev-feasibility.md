# Stri-V Audit 370 — Visual Studio `.slnx` local-dev feasibility (M2a)

## 1) Evidence collection

### Commands used

```bash
dotnet sln build/StriV.Core.slnx list
dotnet build sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj -c Debug -f net10.0
rg -n "StridePlatforms|StrideGraphicsApis|StrideIncludeShaderCompiler|StrideIncludeAudio|StrideIncludeVirtualReality|StrideAssemblyProcessorFramework|StrideAssemblyProcessorBasePath|StrideAssemblyProcessorHash|DesignTimeBuild|BuildingInsideVisualStudio|ImportDirectorySolutionProps|DirectorySolutionPropsPath|before\..*\.slnx\.targets|Directory\.Solution\.props|SolutionName|SolutionPath|SkipCompilerExecution|ProvideCommandLineArgs|ResolvePackageAssets|project.assets.json" build sources docs/stri-v -g '!**/bin/**' -g '!**/obj/**'
rg -n "Stride.Core.AssemblyProcessor\.deps\.json|dll\.config|dll\.hash|deps/AssemblyProcessor|AssemblyProcessor.*Include|Link=|<None Include=.*AssemblyProcessor" sources build -g '!**/bin/**' -g '!**/obj/**'
```

### Files opened/read

- `build/StriV.Core.slnx`
- `build/Stride.sln`
- `sources/Directory.Build.props`
- `sources/Directory.Packages.props`
- `sources/sdk/Directory.Build.props`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.Platform.props`
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`
- `sources/core/Stride.Core/build/Stride.Core.targets`
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `sources/sdk/Stride.Build.Sdk/Stride.Build.Sdk.csproj`
- `sources/sdk/Stride.Build.Sdk.Tests/Sdk/Sdk.targets`
- `build/striv-build-coresmoke-m1g.sh`
- `build/striv-run-coresmoke-m1h.sh`
- `build/striv-build-engine-bepu-m1f.sh`
- `docs/stri-v/building-core.md`

### Light command observations

- `dotnet sln ... list` confirms `build/StriV.Core.slnx` includes the intended 14 core/engine/sample/AP projects.
- `dotnet build` of `Stride.Core.AssemblyProcessor` at `net10.0` succeeds in this environment (many warnings, no errors), producing source-built AP output for property-based routing.

### Confirmation of file modifications

- This audit intentionally avoids engine/source edits.
- Only this report file is added.

### Static/design-time uncertainty declaration

- Visual Studio design-time behavior cannot be directly executed/observed in this Linux sandbox.
- Conclusions about VS-specific import timing/evaluation are evidence-based inferences from MSBuild structure + known symptom patterns and must be validated locally on Windows Visual Studio.

---

## 2) Problem recap

Observed VS symptoms are coherent with **missing Stri‑V property profile during restore/evaluation**:

- `project.assets.json doesn't have a target for net10.0`
- Missing metadata under `bin\Debug\net10.0\ref\...`
- Direct3D11-flavored outputs appearing unexpectedly
- Incomplete/odd Solution Explorer project trees
- Duplicate AP payload link warnings

These point to inconsistent effective values for:

- platform/API profile properties (`StridePlatforms`, `StrideGraphicsApis`)
- feature toggles (`StrideIncludeShaderCompiler`, `StrideIncludeAudio`, `StrideIncludeVirtualReality`)
- AP routing properties (`StrideAssemblyProcessorFramework`, `StrideAssemblyProcessorBasePath`, `StrideAssemblyProcessorHash`)

If VS restore/design-time builds run without that profile, projects can evaluate to default/legacy Stride choices, generating mismatched assets and references.

---

## 3) CLI golden path recap

Current validated Stri‑V scripts explicitly pass the profile and AP routing:

- `StridePlatforms=Linux`
- `StrideGraphicsApis=Vulkan`
- `StrideIncludeShaderCompiler=false`
- `StrideIncludeAudio=false`
- `StrideIncludeVirtualReality=false`
- `StrideAssemblyProcessorFramework=net10.0`
- `StrideAssemblyProcessorBasePath=<source-built AP output dir, absolute + trailing slash>`
- `StrideAssemblyProcessorHash=sourcebuild`

That is visible in `striv-build-coresmoke-m1g.sh` and `striv-build-engine-bepu-m1f.sh`, and documented in `docs/stri-v/building-core.md`.

Source-built AP is required by doctrine and by robustness: checked-in `deps/AssemblyProcessor/*` can be stale/LFS-pointer content and is not treated as authoritative by Stri‑V core bootstrap docs/scripts.

Why CLI works: the profile is supplied at invocation-time, so restore/build graph becomes deterministic for Stri‑V core spine.

---

## 4) MSBuild/Visual Studio property flow audit

### Where defaults are set

- `Stride.Platform.props` sets defaults:
  - `StridePlatforms` defaults by host OS.
  - `StrideGraphicsApis` defaults by detected `StridePlatform` (Windows→Direct3D11, Linux/macOS→Vulkan).

### If `StridePlatforms` is unset on Windows

- It defaults to `Windows`.
- Runtime/TFM conditions downstream may add Windows-targeted behavior and produce non-Stri‑V-core output expectations.

### If `StrideGraphicsApis` is unset on Windows

- It defaults to `Direct3D11`.
- Graphics/API-dependent projects can emit D3D11-specific output paths and assets.

### Which projects become graphics-api-dependent

- Core engine runtime projects in this `.slnx` (`Stride.Graphics`, `Stride.Games`, `Stride.Engine`, `Stride`, sample) are in the affected chain.
- So D3D11 path leakage in VS is expected if no explicit API profile is applied.

### Which properties are required for correct Stri‑V-core evaluation

Must be consistently present during restore + design-time + build:

- `StridePlatforms`
- `StrideGraphicsApis`
- `StrideIncludeShaderCompiler`
- `StrideIncludeAudio`
- `StrideIncludeVirtualReality`
- `StrideAssemblyProcessorFramework`
- `StrideAssemblyProcessorBasePath`
- `StrideAssemblyProcessorHash`

### Which are currently script-only

All eight above are currently passed by CLI scripts; `build/StriV.Core.slnx` itself carries no property profile payload.

---

## 5) Solution/profile mechanism audit

### Option A — `Directory.Build.props` / `Directory.Build.targets`

**Pros**
- Strong for project-level evaluation; VS design-time usually honors it.

**Cons/Risk**
- Scope bleed: a repo- or `sources/`-level file can affect legacy `Stride.sln` unintentionally.
- Guarding by `$(SolutionName)`/`$(SolutionPath)` is fragile in design-time contexts where solution properties can be missing or delayed.

**Verdict**
- Powerful but high contamination risk unless narrowly scoped and conditionally imported.

### Option B — `Directory.Solution.props` / `Directory.Solution.targets`

**Pros**
- Solution-scoped intent.

**Cons**
- VS + `.slnx` import behavior and timing for project design-time evaluation is not confidently provable here.
- Might not solve per-project design-time evaluation if properties do not propagate early enough.

**Verdict**
- Uncertain; local VS validation required before betting M2b on it.

### Option C — `before.StriV.Core.slnx.targets` / `after...`

**Pros**
- Very narrow to specific solution build invocation.

**Cons**
- Primarily CLI solution build hooks; unlikely to fix VS design-time project evaluation consistently.

**Verdict**
- Not sufficient alone for VS friendliness.

### Option D — Stri‑V-specific props imported when opt-in property set

Example: `build/StriV.Core.props` + `StriVCoreProfile=true`.

**Pros**
- Narrow, explicit opt-in.
- Can be consumed by CLI and a bootstrap workflow.
- Avoids global bleed into legacy solutions.

**Cons**
- Requires one reliable injection point (bootstrap/script/user props) to ensure VS sees it early.

**Verdict**
- Good core mechanism for M2b if coupled with bootstrap.

### Option E — Visual Studio bootstrap script (recommended companion)

Example: `build/striv-vs-prepare-core.ps1`.

**Pros**
- Deterministically source-builds AP first.
- Writes/refreshes a local generated props (or sets restore with explicit props) before opening VS.
- Keeps legacy solution untouched.

**Cons**
- Extra step for devs.

**Verdict**
- Best smallest durable path with low risk.

### Option F — Keep `.slnx` CLI-only for now

**Pros**
- Zero implementation risk.

**Cons**
- Does not meet practical local VS dev goal.

**Verdict**
- Not aligned with M2 objective.

---

## 6) AssemblyProcessor local-dev audit

- **Source-built AP output location** should remain project-local build output (e.g., `sources/core/Stride.Core.AssemblyProcessor/bin/<Config>/net10.0/`) and be treated as authoritative for Stri‑V workflows.
- **Auto-build AP during design-time** is risky: design-time builds are frequent, partial, and should avoid side-effect-heavy tool bootstrap.
- **If AP output missing**: AP target path resolution may fall back to SDK/deps probing; that can reintroduce stale `deps/AssemblyProcessor` issues.
- **Portable Windows base-path computation**: use PowerShell `Resolve-Path`/`.Path` and append trailing slash.
- **`StrideAssemblyProcessorHash=sourcebuild`** should be set by profile/bootstrap to avoid hash ambiguity and isolate temp tool copy path.
- **Avoid stale deps payloads**: do not consume `deps/AssemblyProcessor` by default for Stri‑V core; prioritize explicit AP base-path from source build.

---

## 7) Duplicate AssemblyProcessor linked-file warnings audit

Evidence points to packaging/link inclusion patterns:

- `sources/sdk/Stride.Build.Sdk/Stride.Build.Sdk.csproj` packs `deps\AssemblyProcessor/**` into SDK package `tools/AssemblyProcessor/`.
- `sources/core/Stride.Core/Stride.Core.csproj` also packs `deps/AssemblyProcessor/**/*` into `tools/AssemblyProcessor`.
- `Stride.AssemblyProcessor.targets` copies `$(StrideAssemblyProcessorBasePath)*.*` to temp tool cache.

Interpretation:

- Duplicate-link warnings (e.g., `.deps.json`, `.dll.config`, `.dll.hash`) likely arise when multiple payload folders/TFMs are simultaneously represented in item graphs or packaging/link projections.
- These warnings are likely mostly noise for VS project item display/design-time, but they also indicate messy AP payload source-of-truth boundaries.

Can profile avoid old payload folders?
- Yes partially, by forcing explicit `StrideAssemblyProcessorBasePath` to source-built `net10.0` output.
- But legacy pack/include rules still exist and may keep warnings in some contexts.

Fix timing:
- Defer deep cleanup to later; not required for minimal M2b stabilization.

---

## 8) Recommended M2b plan (smallest safe)

### Selected plan

Use **Option D + E**:

1. Add a Stri‑V core profile file (`build/StriV.Core.props`) containing required property defaults.
2. Add VS bootstrap PowerShell (`build/striv-vs-prepare-core.ps1`) that:
   - builds AP from source,
   - computes absolute AP base path with trailing slash,
   - restores `build/StriV.Core.slnx` with Stri‑V profile properties,
   - writes/updates a local generated props file used only by Stri‑V-core workflow (e.g., under `build/` or `.local/`) and prints “open `build/StriV.Core.slnx` in VS now”.
3. Do **not** auto-build AP in design-time targets.
4. Keep legacy `build/Stride.sln` unaffected.

### Proposed properties for profile

- `StridePlatforms=Linux` (for current validated spine)
- `StrideGraphicsApis=Vulkan`
- `StrideIncludeShaderCompiler=false`
- `StrideIncludeAudio=false`
- `StrideIncludeVirtualReality=false`
- `StrideAssemblyProcessorFramework=net10.0`
- `StrideAssemblyProcessorHash=sourcebuild`
- `StrideAssemblyProcessorBasePath` supplied by bootstrap from actual AP output

### Exact command pattern for bootstrap restore

```powershell
dotnet restore build/StriV.Core.slnx `
  -p:StridePlatforms=Linux `
  -p:StrideGraphicsApis=Vulkan `
  -p:StrideIncludeShaderCompiler=false `
  -p:StrideIncludeAudio=false `
  -p:StrideIncludeVirtualReality=false `
  -p:StrideAssemblyProcessorFramework=net10.0 `
  -p:StrideAssemblyProcessorBasePath="<abs AP path with trailing slash>" `
  -p:StrideAssemblyProcessorHash=sourcebuild
```

### Visual Studio user workflow

1. Run `build/striv-vs-prepare-core.ps1` (Debug default).
2. Open `build/StriV.Core.slnx` in Visual Studio.
3. Build selected Stri‑V core projects with the prepared profile state.

### Risks

- VS may still not consume profile during some design-time reload moments (local validation needed).
- Linux-only profile in Windows VS may conflict with native local expectations for some developers; may require a documented alternate profile mode later.

### Validation commands for M2b

- `dotnet build sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj -c Debug -f net10.0`
- `dotnet restore build/StriV.Core.slnx <all Stri-V props>`
- `dotnet build build/StriV.Core.slnx -c Debug <all Stri-V props>`
- Local manual VS validation on Windows (required): open solution, verify no net10.0 assets mismatch / D3D11 leakage for intended profile.

---

## 9) Risk register

| Risk | Area | Likelihood | Impact | Evidence | Mitigation |
| ---- | ---- | ---------: | -----: | -------- | ---------- |
| Stri‑V profile leaks into legacy `Stride.sln` | Build config scope | Medium | High | Global props are broad by default | Keep profile opt-in + bootstrap; avoid repo-global unconditional props. |
| VS design-time ignores solution-level props | VS/MSBuild integration | Medium | High | `.slnx` solution-level import behavior uncertain in this audit | Prefer bootstrap + explicit restore/profile; validate locally on Windows VS. |
| AP output missing before VS loads | Tool bootstrap | High | High | AP path otherwise falls back to deps/sdk probing | Require bootstrap script to build AP first and set AP base path. |
| Auto-building AP during design-time destabilizes VS | Design-time build | Medium | Medium | Design-time builds are frequent and partial | Do not auto-build AP in design-time; keep explicit prep step. |
| Restore assets still target wrong platform/API | Restore graph | Medium | High | Defaults on Windows become `Windows` + `Direct3D11` when unset | Force explicit props in bootstrap/profile for restore/build. |
| Direct3D11 paths still appear | Output evaluation | Medium | Medium | Default `StrideGraphicsApis` on Windows is D3D11 | Keep `StrideGraphicsApis=Vulkan` explicit for Stri‑V core workflow. |
| `.slnx` CLI vs VS behavior diverges | Tooling parity | Medium | Medium | Symptom report already indicates divergence | Treat CLI as golden path; add VS prep and local validation checklist. |
| Duplicate AP link warnings persist | Project item graph | Medium | Low | Multiple AP payload pack/include points exist | Defer deep cleanup; prioritize correctness via explicit AP path routing. |
| Windows devs need `StridePlatforms=Windows` while current profile uses Linux | Local UX | Medium | Medium | Current validated spine is Linux/Vulkan | Document current profile doctrine; consider explicit future alternate local profile variant (out of M2b scope). |

---

## 10) Recommended implementation prompt (for M2b)

> Implement only the minimal local-dev stabilization plan for `build/StriV.Core.slnx`:
>
> 1. Add a Stri‑V-core profile props file under `build/` that defines only the validated Stri‑V core properties (`StridePlatforms=Linux`, `StrideGraphicsApis=Vulkan`, `StrideIncludeShaderCompiler=false`, `StrideIncludeAudio=false`, `StrideIncludeVirtualReality=false`, `StrideAssemblyProcessorFramework=net10.0`, `StrideAssemblyProcessorHash=sourcebuild`) and does not globally affect legacy `Stride.sln` builds.
> 2. Add a Windows PowerShell bootstrap script (e.g., `build/striv-vs-prepare-core.ps1`) that source-builds `Stride.Core.AssemblyProcessor`, computes absolute AP output path with trailing slash, runs `dotnet restore build/StriV.Core.slnx` with the full Stri‑V property set (including `StrideAssemblyProcessorBasePath`), and prints clear instructions to open `build/StriV.Core.slnx` in Visual Studio.
> 3. Do not auto-build AssemblyProcessor in design-time MSBuild targets.
> 4. Do not refactor engine code, do not delete legacy solutions/filters, do not re-enable shader compiler/audio/VR, and preserve existing CLI golden-path scripts.
> 5. Add concise validation steps: AP build command, solution restore/build command with explicit properties, and a local manual Visual Studio checklist to verify net10.0 asset targeting and absence of unintended Direct3D11 profile leakage.
