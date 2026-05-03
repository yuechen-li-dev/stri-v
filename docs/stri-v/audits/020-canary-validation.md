# 020 Canary Validation Audit

Date: 2026-05-02 (UTC)

## 1) Environment and baseline

- OS/environment summary: Ubuntu 24.04.3 LTS container on Linux kernel 6.12.47 x86_64.
- `dotnet --info`: SDK 10.0.107, MSBuild 18.0.11, runtime RID `ubuntu.24.04-x64`, no workloads installed.
- `git branch --show-current`: `work`.
- `git log -1 --pretty=format:'%H%n%s'`:
  - `d0092d4f2d8a45ca3728de40e09114381b7e8dcb`
  - `Merge pull request #2 from yuechen-li-dev/codex/audit-repository-for-windows-runtime-baseline`
- `git status --short` before running commands: empty output.
- Repo already had uncommitted changes: **No**.
- Git LFS installed/initialized: **Yes** (`git-lfs/3.4.1`, filter config present in `git lfs env`).
- LFS files present enough for restore/build attempts: **Appears yes**. `git lfs ls-files` listed tracked artifacts (e.g. `build/.nuget/NuGet.exe`, signing store files), and restore/build progressed far enough to hit compilation/tooling errors rather than missing LFS-pointer failures.

## 2) Commands attempted

Output truncation note: Some long build/test outputs were truncated by the harness token limit; first meaningful failures were preserved.

| Command | Workdir | Duration (approx) | Exit | Truncated? | Classification |
|---|---|---:|---:|---|---|
| `pwd; uname -a; cat /etc/os-release \| head -n 20` | `/workspace/stri-v` | <1s | 0 | No | success |
| `dotnet --info` | `/workspace/stri-v` | <1s | 0 | No | success |
| `git branch --show-current && git log -1 --pretty=format:'%H%n%s' && echo && git status --short` | `/workspace/stri-v` | <1s | 0 | No | success |
| `git lfs version && git lfs env` | `/workspace/stri-v` | <1s | 0 | No | success |
| `git status --short && git lfs ls-files \| head -n 40` | `/workspace/stri-v` | <1s | 0 | No | success |
| `cat build/Stride.Tests.Game.GPU.slnf` | `/workspace/stri-v` | <1s | 0 | No | success |
| `cat .github/workflows/test-windows-game.yml` | `/workspace/stri-v` | <1s | 0 | No | success |
| `time dotnet restore build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D11` | `/workspace/stri-v` | 22.7s | 0 | No | success |
| `time dotnet restore build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D12` | `/workspace/stri-v` | 19.3s | 0 | No | success |
| `time dotnet restore build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Vulkan` | `/workspace/stri-v` | 19.0s | 0 | No | success |
| `time dotnet build build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D11 --no-restore` | `/workspace/stri-v` | 28.3s | 1 | Yes | build graph blocker |
| `time dotnet build build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D12 --no-restore` | `/workspace/stri-v` | 23.6s | 1 | No | build graph blocker |
| `time dotnet build build/Stride.Runtime.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Vulkan --no-restore` | `/workspace/stri-v` | 23.0s | 1 | No | build graph blocker |
| `time dotnet test build/Stride.Tests.Game.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D11 --no-restore` (before test restore) | `/workspace/stri-v` | 2.5s | 1 | No | dependency restore blocker |
| `time dotnet restore build/Stride.Tests.Game.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D11` | `/workspace/stri-v` | 25.1s | 0 | No | success |
| `time dotnet test build/Stride.Tests.Game.slnf -p:StridePlatforms=Windows -p:StrideGraphicsApis=Direct3D11 --no-restore` (after test restore) | `/workspace/stri-v` | 27.6s | 1 | No | build graph blocker |
| `time dotnet restore samples/Physics/BepuSample/BepuSample.sln` | `/workspace/stri-v` | 14.9s | 0 | No | success |
| `time dotnet build samples/Physics/BepuSample/BepuSample.sln --no-restore` | `/workspace/stri-v` | 1.9s | 1 | Yes | expected environment/platform blocker |
| `git status --short` (post-audit) | `/workspace/stri-v` | <1s | 0 | No | success |

## 3) Restore baseline

Attempted and succeeded:

- D3D11 restore on runtime canary succeeded.
- D3D12 restore on runtime canary succeeded.
- Vulkan restore on runtime canary succeeded.

Observed warnings were mostly package pruning/vulnerability advisories (`NU1510`, `NU1901`) and did not block restore.

Interpretation:

- First blocker is **not** package feed credentials, missing workload, or obvious LFS absence.
- Restore baseline is good enough to attempt build graph validation.

## 4) Compile canary validation (`build/Stride.Runtime.slnf`)

### Direct3D11
- Result: **Failed**.
- First meaningful error: `MSB4062` at `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets(187,5)`; could not load `AssemblyProcessorTask` from `/tmp/Stride/AssemblyProcessor/.../Stride.Core.AssemblyProcessor.dll` due to **Bad IL format**.
- Error category: **build graph blocker** (tool/task loading in build pipeline).
- Likely cause: assembly processor task binary produced/resolved in an incompatible format for this Linux environment or current host/runtime expectations.
- Blocks future deletion work?: **Yes** for any change that needs compile canary validation.
- Expected in Linux sandbox?: **Possibly**, but not clearly a generic Windows-targeting-only failure; this is a task-load failure in the core build pipeline.

### Direct3D12
- Result: **Failed**.
- First meaningful error: same `MSB4062` (`AssemblyProcessorTask`, Bad IL format).
- Error category: **build graph blocker**.
- Likely cause: same as D3D11; backend-independent failure point.
- Blocks future deletion work?: **Yes**.
- Expected in Linux sandbox?: **Likely environment/tooling interaction**, but it still behaves as a real canary blocker until validated on Windows.

### Vulkan
- Result: **Failed**.
- First meaningful error: same `MSB4062` (`AssemblyProcessorTask`, Bad IL format).
- Error category: **build graph blocker**.
- Likely cause: same backend-independent build task issue.
- Blocks future deletion work?: **Yes**.
- Expected in Linux sandbox?: **Likely**, but requires Windows confirmation to distinguish environment-only vs hardfork build graph regression.

## 5) Runtime/test canary validation (`build/Stride.Tests.Game.slnf`)

- Initial `dotnet test --no-restore` failed immediately with missing `project.assets.json` for multiple test projects (`NETSDK1004`), so a dedicated restore was run.
- After restore succeeded, `dotnet test --no-restore` proceeded but failed on the same `AssemblyProcessorTask` `MSB4062` Bad IL format error.

Assessment:
- Failure currently indicates a **repo build pipeline blocker** encountered before meaningful runtime test execution.
- It is not yet a pure gameplay/runtime failure signal.

## 6) GPU/backend canary validation (`build/Stride.Tests.Game.GPU.slnf`)

Inspection findings:
- `Stride.Tests.Game.GPU.slnf` contains graphics/engine/UI/physics test projects for Windows.
- `.github/workflows/test-windows-game.yml` runs this on `windows-2025-vs2026`, with:
  - `StridePlatforms=Windows`
  - matrix over `Direct3D11`, `Direct3D12`, `Vulkan`
  - `StrideGraphicsApis` and `StrideGraphicsApi` both set for GPU job
  - `STRIDE_GRAPHICS_SOFTWARE_RENDERING=1`
  - Vulkan-specific setup: install Vulkan SDK + register SwiftShader ICD from NuGet package.

Intended invocation pattern:
- Build then test the GPU solution filter per graphics API, using software rendering fallbacks and special Vulkan setup.

Dependencies implied before serious runs:
- Windows host (workflow is Windows-only).
- Vulkan SDK and SwiftShader ICD registration for Vulkan path.
- LFS assets (`actions/checkout` has `lfs: true`).
- Test artifacts directories, likely GPU-capable or software-rendering compatible drivers/runtime.

Suitability:
- Local Windows: **Suitable after core build canary is healthy**.
- Linux sandbox: **Not suitable for serious execution**; at most limited restore/build probing.

## 7) Physics/sample canary validation (`samples/Physics/BepuSample/BepuSample.sln`)

- Restore: **Succeeded** (with package version drift warnings `NU1603`, advisory warnings).
- Build: **Failed** with `NETSDK1073` (`FrameworkReference 'Microsoft.WindowsDesktop.App' was not recognized`) targeting `net10.0-windows`.

Classification:
- First meaningful blocker: **expected environment/platform blocker** (Linux SDK environment lacks WindowsDesktop targeting support).
- Type: environment-specific (not clearly sample logic failure).
- Canary suitability: still useful as a **Windows-local** sample canary, but weak for Linux CI/sandbox validation.

## 8) Artifact and dirtiness check

- Post-audit `git status --short`: empty output.
- Worktree dirtied by audit commands: **No tracked/untracked changes detected**.
- No cleanup performed (none required).

## 9) Canary verdict

| Candidate | Verdict | Current blocker | Environment-specific? | Recommended role | Next action |
| --------- | ------- | --------------- | --------------------- | ---------------- | ----------- |
| `build/Stride.Runtime.slnf` | Adopt after local Windows validation. | `MSB4062` AssemblyProcessorTask Bad IL format during build. | Unknown/likely yes (Linux interaction suspected), but not proven. | Primary compile/build canary. | Re-run same 3 backend builds on Windows runner/workstation and compare. |
| `build/Stride.Tests.Game.slnf` | Keep as later canary. | Blocked by same upstream compile pipeline failure before tests run. | Probably not test-specific; follows compile blocker. | Runtime/test canary once compile canary passes. | Gate on fixing/clearing runtime compile canary first. |
| `build/Stride.Tests.Game.GPU.slnf` | Keep as later canary. | Requires Windows GPU/software-rendering setup + Vulkan/SwiftShader workflow prep; currently overshadowed by compile blocker. | Yes (Windows-focused workflow). | Backend/GPU regression canary after base compile/test health. | Reproduce CI invocation on Windows including Vulkan SDK + SwiftShader ICD setup. |
| `samples/Physics/BepuSample/BepuSample.sln` | Adopt after local Windows validation. | Linux `NETSDK1073` WindowsDesktop framework reference unsupported. | Yes (platform-specific). | Physics/sample validation canary for Windows desktop track. | Build and run on Windows; also review package version pinning drift warnings. |

## 10) M0d recommendation

**Recommended next audit: Build Graph Repair/Audit.**

Reasoning:
- Restores generally succeeded (so this is not primarily a feed/credential/tool-install blocker).
- First cross-backend blocker on compile and test canaries is a build-pipeline task load failure (`AssemblyProcessorTask` Bad IL format), i.e., build graph/tooling coupling issue.
- A short follow-up Windows confirmation pass is still necessary to classify Linux-only vs cross-platform regression, but the immediate actionable lane is build graph repair analysis around assembly processor task generation/loading.
