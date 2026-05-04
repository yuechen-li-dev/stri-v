# 1030 — M8a Stride.Core.IO 5S Sort validation

## 1) Files changed
- Deleted Android-only source files from active `Stride.Core.IO` source root:
  - `sources/core/Stride.Core.IO/AndroidAssetProvider.cs`
  - `sources/core/Stride.Core.IO/VirtualFileSystem.Android.cs`
  - `sources/core/Stride.Core.IO/ZipFileSystemProvider.cs`
- Deleted vendored ZIP folder from active source root:
  - `sources/core/Stride.Core.IO/System.IO.Compression.Zip/*`
- Updated project file:
  - `sources/core/Stride.Core.IO/Stride.Core.IO.csproj` (removed Android-only `Compile Remove` quarantine rule that referenced deleted ZIP files).

## 2) 5S phase
- This change set is **M8a Sort only**.
- No Set-in-order/Shine/broad refactor performed.
- Applied active removal policy: code proven Android/APK-era and not needed for current Linux/Windows desktop intent was removed instead of left indefinitely behind compile excludes.

## 3) Project inventory
- Compile inventory command output line count: `582` lines (`dotnet msbuild ... -getItem:Compile` output format includes project item metadata blocks).
- Source file inventory in `sources/core/Stride.Core.IO` before changes: `33` files.
- Major areas found:
  - Virtual FS abstraction and providers (`IVirtualFileProvider`, `VirtualFileSystem`, `FileSystemProvider`, `DriveFileProvider`).
  - Watchers/temp/path and stream utilities.
  - Android/APK + vendored ZIP area (`VirtualFileSystem.Android`, `AndroidAssetProvider`, `ZipFileSystemProvider`, `System.IO.Compression.Zip/*`).

## 4) VFS audit
- `IVirtualFileProvider`: **Keep**.
  - Interface shape (`OpenStream`, `FileExists`, `ListFiles`, etc.) remains coherent and is broadly used by active IO providers.
- `VirtualFileSystem` static initialization: **Defer**.
  - Static ctor eagerly initializes `ApplicationData`, `ApplicationCache`, `ApplicationBinary`, etc., which couples to `PlatformFolders` and implicit platform path resolution.
- `ApplicationObjectDatabase` mutable/null-default: **Defer**.
  - Still load-bearing; refactor requires behavior/order redesign beyond Sort.
- `PlatformFolders` coupling: **Defer**.
  - Not changed in M8a; tracked for VFS/static-init follow-up.

## 5) Vendored ZIP / APK audit
- Found under `sources/core/Stride.Core.IO/System.IO.Compression.Zip/*` and Android wrappers/providers.
- Reference graph showed usage confined to Android-guarded code in `Stride.Core.IO` (`#if STRIDE_PLATFORM_ANDROID`).
- Action: **Removed** Android/APK vendored ZIP implementation and Android-only bridge files from active tree.
- Recommendation: if Android support is reintroduced later, prefer BCL `System.IO.Compression` APIs (or module/profile-scoped implementation) instead of restoring monolithic vendored ZIP in core desktop runtime path.
- License handling: removed code retained original header comments previously; project-level license files were not altered.

## 6) Classification table
| Source area/file group | Classification | Reason | Action |
| ---------------------- | -------------- | ------ | ------ |
| `IVirtualFileProvider`, `FileSystemProvider`, `DriveFileProvider`, desktop watcher/temp/path utility files | Keep | Active runtime IO abstractions for desktop graph | Kept unchanged |
| `VirtualFileSystem` static initialization & globals (`ApplicationData`, `ApplicationCache`, `ApplicationBinary`, `ApplicationObjectDatabase`) | Defer | Known design debt but behavior-affecting refactor outside Sort | Documented only |
| `VirtualFileSystem.Android.cs`, `AndroidAssetProvider.cs`, `ZipFileSystemProvider.cs` | Remove | Android-only path, out of Linux/Windows core intent | Deleted |
| `System.IO.Compression.Zip/*` vendored ZIP | Remove | Android/APK-era baggage, referenced only by removed Android-only code | Deleted |

## 7) Changes applied
1. Deleted Android-only providers and Android VFS partial file.
   - Proof: files guarded by `#if STRIDE_PLATFORM_ANDROID`; no desktop path dependency.
   - Risk: only Android-specific functionality removed.
2. Deleted vendored ZIP folder.
   - Proof: references were confined to Android-specific files within `Stride.Core.IO`; no active desktop consumer after removal.
   - Risk: Android APK extraction path no longer available in this core project.
3. Removed stale project quarantine line in csproj.
   - Proof: `Compile Remove="System.IO.Compression.Zip\*.cs"` became obsolete once folder removed.
   - Risk: none for current desktop-focused profile.

## 8) Warning delta
- Focused warning line count before: `60` (grep-based lines; duplicated by msbuild summary echoing warnings).
- Focused warning line count after: `22`.
- Warning codes before: `CS8618`, `CA2022`, `CS8625`, `CS8604`, `CS8600`, `CS8603`, `CS8602`, `CS0652`.
- Warning codes after: `CS8625`, `CS8618`, `CS8604`, `CS8602`, `CS8600`.
- Result: project not zero-warning yet, but large warning reduction via obsolete Android/ZIP removal.

## 9) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet msbuild striv/projects/Stride.Core.IO/Stride.Core.IO.csproj -getItem:Compile > /tmp/striv-m8a-coreio-compile.txt` | 0 | none | Pass | No |
| `find sources/core/Stride.Core.IO -type f | sort > /tmp/striv-m8a-coreio-files.txt` | 0 | none | Pass | No |
| key-area `rg -n ...` scans | 0 | none | Pass | No |
| ZIP reference `rg -n ...` scan | 0 | none | Pass | No |
| VFS/global reference `rg -n ...` scan | 0 | none | Pass | No |
| Before baseline build (`dotnet build ... StriVWarningFocusProject=Stride.Core.IO`) | 0 | CS8618/CA2022 in `Stride.Core.IO` | Pass | No |
| After validation build (`dotnet build ... StriVWarningFocusProject=Stride.Core.IO`) | 0 | residual CS8618/CS8604 in non-removed files | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.IO` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none (tests passed) | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | repository-wide warnings in dependencies, tests completed | Pass | Yes (console truncated in capture) |
| `./striv/build/striv-build-core.sh` | 0 | repository-wide existing warnings | Pass | Yes (console truncated in capture) |

## 10) Project standard draft (first pass)
- `Stride.Core.IO` should contain runtime-necessary cross-platform desktop IO abstractions/providers and VFS-facing primitives.
- Android/APK extraction helpers and vendored ZIP internals do **not** belong in active desktop-focused core source root without profile/module gating.
- Future VFS refactor should preserve provider interface contract and deterministic behavior while reducing implicit static initialization order hazards.
- Do not reintroduce platform-specialized legacy code into `Stride.Core.IO` active root unless tied to explicit module/profile boundary and validated consumer graph.

## 11) Recommended next step
- **M8b Set-in-order focused on VFS/static initialization design**:
  - split eager static initialization from provider registration/bootstrap,
  - make `ApplicationObjectDatabase` initialization explicit/non-null-safe,
  - reduce hidden `PlatformFolders` side effects for deterministic tests.
