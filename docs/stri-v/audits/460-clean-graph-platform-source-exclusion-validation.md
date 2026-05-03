# 460 Clean Graph Platform Source Exclusion Validation

## 1. Files changed
- `striv/projects/Stride.Core.IO/Stride.Core.IO.csproj`
- `docs/stri-v/audits/460-clean-graph-platform-source-exclusion-validation.md`

## 2. Blocker recap
- First failing project before this repair: `striv/projects/Stride.Core.IO/Stride.Core.IO.csproj`.
- Failing source file: `sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs`.
- Missing types/namespaces reported: `Android`, `Context`.
- Why this is out-of-profile: `ApkExpansionSupport.cs` is Android APK expansion support and is not part of the Linux desktop + Vulkan + SDL clean profile.

## 3. Source exclusion changes
- Changed clean project file: `striv/projects/Stride.Core.IO/Stride.Core.IO.csproj`.
- Added a narrow compile removal:
  - `../../../sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs`
- Added explanatory comment that this file is Android-only and outside Linux desktop clean profile.
- Additional exclusions beyond `ApkExpansionSupport.cs`: none in this change.
- Safety rationale: removing Android APK-specific code from Linux clean profile does not reduce Linux desktop runtime capability and prevents Android namespace/type resolution failures in clean graph builds.

## 4. Platform source scan
- Command used:
  - `rg -n "namespace Android|using Android|Android\\.|UIKit|Foundation|CoreMotion|CoreLocation|Windows\\.UI|Windows\\.Foundation|STRIDE_PLATFORM_ANDROID|STRIDE_PLATFORM_IOS|STRIDE_PLATFORM_UWP|GamePlatformAndroid|GamePlatformiOS|GamePlatformUWP|InputSourceAndroid|InputSourceiOS|InputSourceUWP|GameContextAndroid|GameContextiOS|GameContextUWP" sources/core/Stride.Core.IO sources/engine/Stride.Games sources/engine/Stride.Input sources/engine/Stride.Graphics sources/engine/Stride.Engine sources/engine/Stride.BepuPhysics/Stride.BepuPhysics`
- Findings (representative, from command output):
  - Android/iOS/UWP files in `sources/engine/Stride.Games`, including:
    - `GameContextAndroid.cs` (`#if STRIDE_PLATFORM_ANDROID`)
    - `Starter/StrideActivity.cs` (`#if STRIDE_PLATFORM_ANDROID`)
    - `GameContextiOS.cs` (`#if STRIDE_PLATFORM_IOS`)
    - `iOS/GamePlatformiOS.cs` (`#if STRIDE_PLATFORM_IOS`)
    - `WindowsStore/GameWindowUWP.cs` (`#if STRIDE_PLATFORM_UWP`, `using Windows.*`)
    - `WindowsStore/GamePlatformUWP.cs` (`#if STRIDE_PLATFORM_UWP`)
  - `sources/engine/Stride.Games/GameContext.cs` includes UWP `#if STRIDE_PLATFORM_UWP` sections.
  - `sources/engine/Stride.Graphics/Direct3D/SwapChainGraphicsPresenter.Direct3D.cs` includes UWP-guarded code and `Windows.UI.*` usage.
- Guarding/exclusion assessment:
  - Most platform-specific files discovered are already symbol-guarded with `#if STRIDE_PLATFORM_ANDROID`, `#if STRIDE_PLATFORM_IOS`, or `#if STRIDE_PLATFORM_UWP`.
  - No additional preemptive exclusions were added in this pass because the requested convergence policy favors first-build-evidence and narrow edits.

## 5. Validation results
1) Command:
- `./striv/build/striv-build-core.sh`
- Exit code: `1`
- First meaningful warning/error:
  - Warning first observed during solution build: `NU1510` package-pruning warnings.
  - First meaningful blocking error:
    - `/workspace/stri-v/sources/engine/Stride/Graphics/StandardImageHelper.Desktop.cs(10,7): error CS0246: The type or namespace name 'FreeImageAPI' could not be found`
- Pass/fail: **FAIL**
- Output truncated: **Yes** (tool output truncated due size limits)

2) Command:
- `rg -n "namespace Android|using Android|Android\\.|UIKit|Foundation|CoreMotion|CoreLocation|Windows\\.UI|Windows\\.Foundation|STRIDE_PLATFORM_ANDROID|STRIDE_PLATFORM_IOS|STRIDE_PLATFORM_UWP|GamePlatformAndroid|GamePlatformiOS|GamePlatformUWP|InputSourceAndroid|InputSourceiOS|InputSourceUWP|GameContextAndroid|GameContextiOS|GameContextUWP" sources/core/Stride.Core.IO sources/engine/Stride.Games sources/engine/Stride.Input sources/engine/Stride.Graphics sources/engine/Stride.Engine sources/engine/Stride.BepuPhysics/Stride.BepuPhysics`
- Exit code: `0`
- First meaningful warning/error: none (matches returned)
- Pass/fail: **PASS**
- Output truncated: **Yes** (very large output truncated)

## 6. First new blocker
- Project: `striv/projects/Stride/Stride.csproj`
- Error location:
  - `sources/engine/Stride/Graphics/StandardImageHelper.Desktop.cs(10,7)`
  - `sources/engine/Stride/Graphics/StandardImageHelper.Desktop.cs(246,54)`
- Error:
  - `CS0246` missing namespace/type: `FreeImageAPI`, `FreeImageBitmap`.
- Likely cause:
  - Desktop image helper expects FreeImage dependency/reference not present in current clean graph dependency set.
- Smallest proposed next repair:
  - Clean profile/package/reference repair for `Stride` project to either provide the required FreeImage package/reference or narrowly exclude/replace `StandardImageHelper.Desktop.cs` path under Linux clean profile if that functionality is intentionally out-of-scope.

## 7. Test results
- Tests were **not run** because build failed, per instruction.

## 8. Worktree status
- Command: `git status --short`
- Output:
  - ` M striv/projects/Stride.Core.IO/Stride.Core.IO.csproj`
  - `?? docs/stri-v/audits/460-clean-graph-platform-source-exclusion-validation.md`

## 9. Recommended next task
- **package/reference repair**
  - Resolve missing FreeImage dependency or adjust clean profile source selection for `StandardImageHelper.Desktop.cs` in `striv/projects/Stride/Stride.csproj` based on intended Linux desktop clean scope.
