# M10n Stride.Input runtime smoke validation

## 1) Files changed
- `striv/tests/Stride.Input.Tests/InputRuntimeSmokeTests.cs`
- `docs/stri-v/audits/1000+/1270-m10n-stride-input-runtime-smoke-validation.md`

## 2) Task scope
This task adds targeted runtime smoke validation for the cleaned `Stride.Input` abstraction layer.

It does **not** add new architecture and does **not** introduce InputMan/action maps/rebinding/gameplay semantics.

## 3) Simulated input smoke
Added deterministic, platform-independent tests in `InputRuntimeSmokeTests`:
- `SimulatedInputSource_CanRegisterAndUpdateThroughInputManager`
- `SimulatedKeyboardOrMouse_StateChangeFlowsThroughManager`
- `SimulatedGamePad_StateChangeFlowsThroughManager`

Validated behaviors:
- simulated source registration + update ticks through `InputManager` without exceptions;
- keyboard/mouse state mutations (`SimulateDown/Up`, mouse button + position) reflected through manager-level state;
- simulated gamepad button/axis event flow reaches manager event stream and update loop remains stable.

Limitations:
- gamepad per-button down/up state semantics in current abstraction are smoke-validated via event flow rather than strict pressed-bit lifecycle expectations.

## 4) SDL probe/smoke
Automated SDL runtime smoke is deferred.

Reason:
- deterministic, CI-safe construction/disposal of SDL input source requires stable SDL native/window host availability;
- this lane intentionally avoids flaky native/runtime tests.

CI requirement stance:
- not required in CI until a stable runtime host validation harness exists.

## 5) RawInput probe/smoke
Automated Windows RawInput smoke is deferred.

Reason:
- current environment is not guaranteed Windows;
- safe automation should be Windows-gated and avoid physical-device assumptions.

Manual plan:
- run Windows-only headful/manual validation that initializes the RawInput path, verifies keyboard registration and key event propagation.

## 6) Tests
Added tests:
- `SimulatedInputSource_CanRegisterAndUpdateThroughInputManager`: source/device registration and update smoke.
- `SimulatedKeyboardOrMouse_StateChangeFlowsThroughManager`: keyboard and mouse state/event propagation smoke.
- `SimulatedGamePad_StateChangeFlowsThroughManager`: simulated gamepad event propagation smoke.

## 7) Focused warning sustain
- `./striv/build/striv-check-focused-project.sh Stride.Input` => PASS, zero focused warnings.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input` => PASS.

## 8) Validation results
1. Command: `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

2. Command: `./striv/build/striv-check-focused-project.sh Stride.Input`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

3. Command: `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

4. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `warning CS0436` in `Stride.Rendering/Properties/AssemblyInfo.cs`
   - Pass/fail: PASS
   - Output truncated: no

5. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `warning CS8604` in `StriV.AssetPipeline/AssetPipeline.cs`
   - Pass/fail: PASS
   - Output truncated: no

6. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

7. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

8. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: `warning CS1030` in `sources/core/Stride.Core/Storage/ObjectIdBuilder.cs`
   - Pass/fail: PASS
   - Output truncated: no

## 9) Deferred work
- SDL real runtime/window validation.
- Windows RawInput validation.
- controller/XInput policy.
- deletion of `Obsolete/WindowsControllers`.
- future `InputMan`.

## 10) Recommended next task
Recommended next task: Windows RawInput manual smoke validation on a Windows host, to close the remaining platform-specific runtime confidence gap without introducing flaky CI automation.
