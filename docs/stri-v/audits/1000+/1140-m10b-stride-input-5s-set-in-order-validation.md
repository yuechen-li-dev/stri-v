# 1140 - M10b Stride.Input 5S Set-in-order validation

## 1) Files changed
- Reorganized active files into: `Abstractions/`, `Core/`, `Devices/`, `Events/`, `Layouts/`, `Sources/`, `Utilities/`.
- Representative moves:
  - `InputManager.cs` -> `Core/InputManager.cs`
  - `InputSourceFactory.cs` -> `Core/InputSourceFactory.cs`
  - `IInputSource.cs` -> `Abstractions/IInputSource.cs`
  - `PointerEvent.cs` -> `Events/PointerEvent.cs`
  - `GamePadLayout*.cs` -> `Layouts/`
  - `SDL/*.cs` -> `Sources/SDL/`
  - `Simulated/*.cs` -> `Sources/Simulated/`
  - `Windows/InputSourceWindowsRawInput.cs` -> `Sources/WindowsRawInput/InputSourceWindowsRawInput.cs`
  - `Windows/RawInput/*.cs` -> `Sources/WindowsRawInput/RawInput/`
- Modified comments/docs:
  - `Core/InputManager.cs`
  - `Sources/SDL/InputSourceSDL.cs`
  - `Sources/WindowsRawInput/InputSourceWindowsRawInput.cs`
  - `Sources/Simulated/InputSourceSimulated.cs`
  - `Devices/Pointer/PointerDeviceState.cs`
  - `Devices/Mouse/MouseDeviceState.cs`
  - `Compatibility/VirtualButtonConfigShim.cs`
- Added: `Obsolete/WindowsControllers/README.md`

## 2) 5S phase
- M10a = Sort (scope reduction, obsolete exclusion).
- M10b = Set in order (navigation map, ownership clarity, low-risk file organization).
- Shine/nullability cleanup is intentionally deferred.

## 3) Organization plan
- Folder zones established:
  - `Abstractions`: interfaces/contracts.
  - `Core`: `InputManager`, source factory/base, module entry.
  - `Devices`: typed device/state implementations.
  - `Events`: runtime event payload types.
  - `Layouts`: gamepad layout mapping descriptions.
  - `Sources`: backend providers (`SDL`, `WindowsRawInput`, `Simulated`).
  - `Utilities`: helpers.
  - `Compatibility`: API shims.
  - `Obsolete`: non-compiled inventory.
- Namespace policy: preserved `Stride.Input` for moved files.
- Project include/exclude policy unchanged and still explicit:
  - include `**/*.cs`
  - exclude `**/ToBeDeleted/**` and `**/Obsolete/**`.
- Public API names/signatures were not changed.

## 4) Input architecture map
- `InputManager` remains central coordinator for source registration, per-frame update, event routing, and canonical device property refresh.
- Source flow: `Sources` collection -> source initialize/scan/update -> device add/remove callbacks -> manager device lists/properties.
- SDL backend: desktop SDL device discovery + hotplug lifecycle ownership.
- Windows RawInput backend: Win32 raw keyboard registration and keyboard event translation.
- Simulated backend: deterministic programmatic source for tests/tooling.
- Device/state/event roles:
  - abstractions define public contracts,
  - device/state classes hold mutable per-frame state,
  - event classes carry frame events.
- Compatibility shim role: virtual-button API compatibility surface only, no runtime subsystem.
- Obsolete status: DirectInput/XInput inventory remains excluded from compile and documented as inactive scope.

## 5) Documentation/comments added
- Added `InputManager` ownership/lifecycle remarks for coordination, source flow, and compatibility surface.
- Added backend ownership remarks for SDL, Windows RawInput, and Simulated sources.
- Added device state intent comments in pointer/mouse state classes.
- Expanded virtual-button shim comment with explicit “compatibility-only” and “do not expand without decision” guidance.
- Added obsolete README to prevent accidental reactivation.

## 6) Zero-risk refactors
- File relocation only (namespace-preserving).
- No behavior logic changes.
- No registration order or update order changes.
- No platform selector changes.
- No project-compile boundary changes beyond existing excludes.

## 7) Behavior compatibility
- No behavior changes intended.
- No public API rename/signature changes.
- No platform selection changes.
- No restored Android/iOS/UWP/VirtualButton runtime.
- No restored DirectInput/XInput runtime backends.
- Build/test evidence captured below.

## 8) Focused warning snapshot
- Focused warning lines after M10b: **164**.
- Top warning codes:
  - CS8618: 70
  - CS8602: 22
  - CS8601: 22
  - CS8604: 12
  - CS8600: 12
- Comparison to M10a prior snapshot (164): **unchanged**.

## 9) Deferred work
- Shine/nullability warning cleanup.
- Windows RawInput runtime validation on target runtime hosts.
- SDL runtime validation on target desktop runtimes.
- Explicit controller-support decision (and XInput strategy).
- Potential deletion of `Obsolete/WindowsControllers` after decision.
- Optional follow-up Set-in-order pass if additional map/documentation gaps are found during Shine.

## 10) Validation results
1. `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input`
   - Exit: 0
   - First meaningful warning: `CS8765` in `Devices/Sensors/Direction.cs`
   - Pass/Fail: Pass (with warnings)
   - Output truncated: No
2. `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input`
   - Exit: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
3. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
4. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
5. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit: 0
   - First meaningful warning/error: none (1 known skip)
   - Pass/Fail: Pass
   - Output truncated: No
6. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No
7. `./striv/build/striv-build-core.sh`
   - Exit: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: Yes (terminal display truncated; command completed successfully)

## 11) Recommended next task
- **Recommend M10c Shine for `Stride.Input`** (nullability/warning cleanup), since Set-in-order map is now established and focused warning count is stable.
