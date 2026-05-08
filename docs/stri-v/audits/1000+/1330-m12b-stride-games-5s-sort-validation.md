# M12b Stride.Games 5S Sort Validation

## 1) Files changed

### Moved (active organization)
- `striv/projects/Stride.Games/GameBase.cs` -> `striv/projects/Stride.Games/Host/GameBase.cs`
- `striv/projects/Stride.Games/GameContext.cs` -> `striv/projects/Stride.Games/Host/GameContext.cs`
- `striv/projects/Stride.Games/GameContextFactory.cs` -> `striv/projects/Stride.Games/Host/GameContextFactory.cs`
- `striv/projects/Stride.Games/GameContextHeadless.cs` -> `striv/projects/Stride.Games/Host/GameContextHeadless.cs`
- `striv/projects/Stride.Games/GamePlatform.cs` -> `striv/projects/Stride.Games/Host/GamePlatform.cs`
- `striv/projects/Stride.Games/GameTime.cs` -> `striv/projects/Stride.Games/Host/GameTime.cs`
- `striv/projects/Stride.Games/GameUnhandledExceptionEventArgs.cs` -> `striv/projects/Stride.Games/Host/GameUnhandledExceptionEventArgs.cs`
- `striv/projects/Stride.Games/LaunchParameters.cs` -> `striv/projects/Stride.Games/Host/LaunchParameters.cs`
- `striv/projects/Stride.Games/GameSystemBase.cs` -> `striv/projects/Stride.Games/Systems/GameSystemBase.cs`
- `striv/projects/Stride.Games/GameSystemCollection.cs` -> `striv/projects/Stride.Games/Systems/GameSystemCollection.cs`
- `striv/projects/Stride.Games/GameSystemState.cs` -> `striv/projects/Stride.Games/Systems/GameSystemState.cs`
- `striv/projects/Stride.Games/IGameSystemBase.cs` -> `striv/projects/Stride.Games/Systems/IGameSystemBase.cs`
- `striv/projects/Stride.Games/IGameSystemCollection.cs` -> `striv/projects/Stride.Games/Systems/IGameSystemCollection.cs`
- `striv/projects/Stride.Games/IContentable.cs` -> `striv/projects/Stride.Games/Systems/IContentable.cs`
- `striv/projects/Stride.Games/IDrawable.cs` -> `striv/projects/Stride.Games/Systems/IDrawable.cs`
- `striv/projects/Stride.Games/IUpdateable.cs` -> `striv/projects/Stride.Games/Systems/IUpdateable.cs`
- `striv/projects/Stride.Games/GameWindow.cs` -> `striv/projects/Stride.Games/Windowing/GameWindow.cs`
- `striv/projects/Stride.Games/GameWindowHeadless.cs` -> `striv/projects/Stride.Games/Windowing/GameWindowHeadless.cs`
- `striv/projects/Stride.Games/IMessageLoop.cs` -> `striv/projects/Stride.Games/Windowing/IMessageLoop.cs`
- `striv/projects/Stride.Games/GraphicsDeviceManager.cs` -> `striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceManager.cs`
- `striv/projects/Stride.Games/GraphicsDeviceInformation.cs` -> `striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceInformation.cs`
- `striv/projects/Stride.Games/IGraphicsDeviceFactory.cs` -> `striv/projects/Stride.Games/GraphicsBridge/IGraphicsDeviceFactory.cs`
- `striv/projects/Stride.Games/IGraphicsDeviceManager.cs` -> `striv/projects/Stride.Games/GraphicsBridge/IGraphicsDeviceManager.cs`
- `striv/projects/Stride.Games/GameGraphicsParameters.cs` -> `striv/projects/Stride.Games/GraphicsBridge/GameGraphicsParameters.cs`
- `striv/projects/Stride.Games/PreparingDeviceSettingsEventArgs.cs` -> `striv/projects/Stride.Games/GraphicsBridge/PreparingDeviceSettingsEventArgs.cs`
- `striv/projects/Stride.Games/GameWindowRenderer.cs` -> `striv/projects/Stride.Games/GraphicsBridge/GameWindowRenderer.cs`
- `striv/projects/Stride.Games/GraphicsDeviceManagerProfilingKeys.cs` -> `striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceManagerProfilingKeys.cs`
- `striv/projects/Stride.Games/ListBoundExtensions.cs` -> `striv/projects/Stride.Games/Utilities/ListBoundExtensions.cs`

### Quarantined (mobile/UWP)
- `striv/projects/Stride.Games/GameContextAndroid.cs` -> `striv/projects/Stride.Games/Obsolete/Mobile/GameContextAndroid.cs`
- `striv/projects/Stride.Games/GameContextiOS.cs` -> `striv/projects/Stride.Games/Obsolete/Mobile/GameContextiOS.cs`
- `striv/projects/Stride.Games/GameContextUWP.cs` -> `striv/projects/Stride.Games/Obsolete/Mobile/GameContextUWP.cs`
- `striv/projects/Stride.Games/Platforms/Android/Resources/Layout/stride_popup_edittext.xml` -> `striv/projects/Stride.Games/Obsolete/Mobile/Platforms/Android/Resources/Layout/stride_popup_edittext.xml`
- `striv/projects/Stride.Games/Starter/StrideActivity.cs` -> `striv/projects/Stride.Games/Obsolete/Mobile/Starter/StrideActivity.cs`

### Modified/New
- Modified: `striv/projects/Stride.Games/Stride.Games.csproj`
- Added: `striv/projects/Stride.Games/Obsolete/Mobile/README.md`

## 2) 5S phase
- M12a was planning-only Sort audit.
- M12b is Sort implementation (physical organization and quarantine).
- No Set-in-order deep architecture refactor and no Shine warning cleanup performed.

## 3) Project purpose
`Stride.Games` remains the minimal desktop runtime host contract: game loop lifecycle/tick/run flow, timing primitives, host context abstraction, and host-facing system orchestration.

## 4) Organization applied
- Created folder groupings: `Host/`, `Systems/`, `Windowing/`, `GraphicsBridge/`, `Utilities/`, and `Obsolete/Mobile/`.
- Preserved existing namespaces and public type names/signatures; only file paths changed.
- Updated project compile policy: wildcard include now excludes `Obsolete/**/*.cs` and `ToBeDeleted/**/*.cs`.
- Preserving namespaces avoids downstream break risk for `Stride.Input`, `Stride.Rendering`, and other consumers with concrete type references.

## 5) Mobile/UWP quarantine
- Quarantined Android/iOS/UWP context and Android starter/resource files to `Obsolete/Mobile/**`.
- Rationale: out-of-scope for desktop-first Stri-V boundary in this pass.
- Compile exclusion confirms they are non-active reference-only assets.
- Added `Obsolete/Mobile/README.md` documenting scope and non-compiled status.

## 6) Dependency compatibility
- Reference scan confirms dependency-sensitive symbols still present with unchanged type names/namespaces.
- `Stride.Input` still references `GameContextAndroid`, `GameContextiOS`, `GameContextUWPXaml`, `GameContextUWPCoreWindow`, etc., but build/test remained green because these references are conditionally compiled in non-desktop paths.
- `Stride.Rendering` build succeeded against the reorganized `Stride.Games`.
- No public API churn introduced.

## 7) Warning snapshot
- Focused `Stride.Games` warning lines after Sort: **292**.
- Top warning codes:
  - CS8618: 122
  - CS8622: 66
  - CS8625: 46
  - CS8603: 22
  - CS8601: 10
- Comparison to M12a baseline (292): **no net change**.

## 8) Build/test validation

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental 2>&1 | tee /tmp/striv-m12b-games-sort-build.log` | 0 | `warning CS8625` in `IGamePlatform.cs` | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental 2>&1 | tee /tmp/striv-m12b-games-slnx-build.log` | 0 | `warning CS8625` in `IGamePlatform.cs` | Pass | Yes |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one known skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal \|\| true` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | Yes |

## 9) Deferred work
- M12c Set-in-order for clearer host/windowing/graphics-bridge boundaries and remaining stragglers.
- Shine phase warning cleanup (not done in M12b).
- Future splits (later projects):
  - generic windowing abstraction,
  - desktop backend implementations,
  - graphics bridge/presentation ownership,
  - final deletion of mobile quarantine.
- Dominatus/lifecycle integration remains deferred.

## 10) Recommended next task
Proceed with **M12c Set-in-order for `Stride.Games`**, unless a downstream blocker emerges.
