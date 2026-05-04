# 1040 - M8b Core.IO 5S Set in order validation

## 1) Files changed

### Moved files
- `sources/core/Stride.Core.IO/DriveFileProvider.cs` -> `sources/core/Stride.Core.IO/Providers/DriveFileProvider.cs`
- `sources/core/Stride.Core.IO/FileSystemProvider.cs` -> `sources/core/Stride.Core.IO/Providers/FileSystemProvider.cs`
- `sources/core/Stride.Core.IO/IVirtualFileProvider.cs` -> `sources/core/Stride.Core.IO/Providers/IVirtualFileProvider.cs`
- `sources/core/Stride.Core.IO/VirtualFileProviderBase.cs` -> `sources/core/Stride.Core.IO/Providers/VirtualFileProviderBase.cs`
- `sources/core/Stride.Core.IO/VirtualFileSystem.cs` -> `sources/core/Stride.Core.IO/VirtualFileSystem/VirtualFileSystem.cs`
- `sources/core/Stride.Core.IO/DirectoryWatcher.cs` -> `sources/core/Stride.Core.IO/Watching/DirectoryWatcher.cs`
- `sources/core/Stride.Core.IO/DirectoryWatcher.Desktop.cs` -> `sources/core/Stride.Core.IO/Watching/DirectoryWatcher.Desktop.cs`
- `sources/core/Stride.Core.IO/FileEvent.cs` -> `sources/core/Stride.Core.IO/Watching/FileEvent.cs`
- `sources/core/Stride.Core.IO/FileEventChangeType.cs` -> `sources/core/Stride.Core.IO/Watching/FileEventChangeType.cs`
- `sources/core/Stride.Core.IO/VirtualWatcherChangeTypes.cs` -> `sources/core/Stride.Core.IO/Watching/VirtualWatcherChangeTypes.cs`
- `sources/core/Stride.Core.IO/NativeLockFile.cs` -> `sources/core/Stride.Core.IO/Utilities/NativeLockFile.cs`
- `sources/core/Stride.Core.IO/TemporaryDirectory.cs` -> `sources/core/Stride.Core.IO/Utilities/TemporaryDirectory.cs`
- `sources/core/Stride.Core.IO/TemporaryFile.cs` -> `sources/core/Stride.Core.IO/Utilities/TemporaryFile.cs`

### Modified files
- `sources/core/Stride.Core.IO/VirtualFileSystem/VirtualFileSystem.cs`
- `sources/core/Stride.Core.IO/Providers/IVirtualFileProvider.cs`
- `sources/core/Stride.Core.IO/Providers/FileSystemProvider.cs`
- `sources/core/Stride.Core.IO/Providers/DriveFileProvider.cs`

## 2) 5S phase
- M8a performed **Sort** (deletion of Android ZIP-only code and explicit debt identification).
- M8b performs **Set in order** (navigation, grouping, and invariant documentation only).
- Shine/warning cleanup and runtime refactors are intentionally deferred.

## 3) Organization plan
- Created grouping folders: `Providers/`, `VirtualFileSystem/`, `Watching/`, `Utilities/`.
- Namespace policy: kept existing `Stride.Core.IO` namespace for all moved public types to avoid API breakage.
- Project include behavior: no csproj compile-item rewrite required; SDK/default globbing keeps `**/*.cs` included after file moves.

## 4) VFS architecture map
- `IVirtualFileProvider`: provider contract for mountable backing stores, including stream and listing semantics.
- Provider classes:
  - `FileSystemProvider`: OS filesystem-backed provider rooted at a base path.
  - `DriveFileProvider`: virtual mount for full-host drive access (tooling-oriented).
- `VirtualFileSystem`: process-wide static provider registry and mount resolution surface.
- `ApplicationObjectDatabase`: still globally mutable/static and initialized externally; documented as an explicit hazard if left null/default.
- `PlatformFolders`: still indirectly coupled by eager static initialization inside `VirtualFileSystem` constructor.
- Hidden initialization hazards documented:
  - accessing static provider members triggers eager static constructor side effects,
  - platform folder state mutation occurs during this implicit initialization.

## 5) Documentation changes
- `VirtualFileSystem`: added comments on ownership, eager static initialization, platform folder coupling, and ApplicationObjectDatabase hazard.
- `IVirtualFileProvider`: refined contract docs for path expectations, caller stream ownership, and listing semantics.
- `FileSystemProvider`: clarified OS filesystem backing and base-path rebinding intent.
- `DriveFileProvider`: clarified virtual-drive behavior and stream ownership expectations.

## 6) Behavior compatibility
- No behavior changes intended.
- No source deletion.
- No public API renames.
- No namespace breaking changes.
- Build/test evidence captured in validation commands below.

## 7) Deferred work
- VFS static initialization refactor (remove hidden side effects / explicit initialization flow).
- Explicit `ApplicationObjectDatabase` lifecycle/ownership initialization.
- Potential `System.IO.Abstractions` migration.
- Shine warning cleanup pass.
- Additional deeper namespace/folder cleanup if future modules split out object-database providers.

## 8) Validation results
1. `dotnet msbuild striv/projects/Stride.Core.IO/Stride.Core.IO.csproj -getItem:Compile > /tmp/striv-m8b-coreio-compile.txt`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

2. `dotnet build striv/projects/Stride.Core.IO/Stride.Core.IO.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.IO`
   - Exit code: 0
   - First meaningful warning/error: `DriveFileProvider.cs(32,71) warning CS8604`
   - Pass/fail: pass (warnings expected/deferred)
   - Output truncated: no

3. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: upstream warning in `StriV.AssetPipeline` (`CS8604`) during build
   - Pass/fail: pass
   - Output truncated: no

4. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none (no-build)
   - Pass/fail: pass
   - Output truncated: yes (when run in chained command)

5. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none (no-build)
   - Pass/fail: pass
   - Output truncated: yes (when run in chained command)

6. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing upstream warnings from engine projects
   - Pass/fail: pass
   - Output truncated: yes

7. `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: existing `Stride.Core.AssemblyProcessor` warnings (e.g. CS1030/CS8618 family)
   - Pass/fail: pass
   - Output truncated: yes

## 9) Recommended next task
- **M8c Shine for `Stride.Core.IO`** with focused warning-triage while preserving M8b organization and invariants.
