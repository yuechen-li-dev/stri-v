# M10d – Stride.Input InputManager lifecycle tests validation

## 1) Files changed
- `striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj`
- `striv/tests/Stride.Input.Tests/InputManagerLifecycleTests.cs`
- `striv/StriV.Core.slnx`
- `docs/stri-v/audits/1000+/1160-m10d-stride-input-lifecycle-tests-validation.md`

## 2) Task scope
M10d is a **test-first stabilization pass** for `Stride.Input`, specifically around `InputManager` lifecycle/nullability risk areas. This pass intentionally does **not** perform Shine pass 2 warning cleanup; it establishes behavior-locking tests so M10e can safely tighten nullability and lifecycle ownership with regression protection.

## 3) InputManager lifecycle map
Observed behavior from source inspection and tests:
- Construction: `InputManager()` initializes gesture/source collections and leaves compatibility property `VirtualButtonConfigSet` unset (`null`) by default.
- Source registration flow: adding to `Sources` triggers `SourcesOnCollectionChanged` add path, wires device collection callbacks, and calls `source.Initialize(this)`.
- Device ownership: source `Devices` add/remove callbacks drive `OnInputDeviceAdded/Removed`, updating device lists and `Has*` convenience properties.
- Update flow: `Update(GameTime)` can execute before `Initialize(GameContext)` if no unsupported event routing path is hit; update loops sources/devices and processes events.
- `gameContext` lifecycle: assigned in `Initialize(GameContext)` and consumed by `AddSources()`. Default constructor leaves it unset.
- VirtualButton surface: `VirtualButtonConfigSet` remains compatibility-only and nullable; virtual button runtime was not restored.

## 4) Test project / test design
- Project added: `striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj`.
- Framework/deps: xUnit + `Microsoft.NET.Test.Sdk`; project references to `Stride.Input`, `Stride.Games`, and `Stride.Core`.
- Design choice: used `Sources/Simulated` (`InputSourceSimulated` and simulated devices) to keep tests deterministic and platform-independent (no SDL window, no RawInput).
- Test seams: no production seam changes were required; existing public API surface was sufficient.

## 5) Tests added
- `InputManager_Constructs_WithVirtualButtonCompatibilitySurface`
  - Locks default constructor expectations including nullable compatibility surface.
  - Matters for M10e because constructor nullability warnings can now be fixed with baseline behavior preserved.
- `InputManager_AddsSimulatedSource_AndUpdatesWithoutPlatformBackend`
  - Locks source add + one update tick without native backend dependency.
  - Matters for M10e lifecycle refactors around source/update ownership.
- `InputManager_DeviceCallbacks_RegisterAndRemoveDevices`
  - Locks source-driven add/remove callback ownership and collection consistency.
  - Matters for nullability and ownership changes in registration paths.
- `InputManager_Update_DoesNotRequireGameContextBeforeInitialization`
  - Locks current behavior that update can run pre-`Initialize` in basic conditions.
  - Matters for honest nullability constraints around `gameContext`.
- `InputManager_SourceRegistration_DuplicateAddFailsPredictably`
  - Locks duplicate source add failure mode (`InvalidOperationException`).
  - Matters for safe cleanup of source registration warnings without changing semantics.

## 6) Production changes
No production code changes were made.

## 7) Focused warning snapshot
- Command lane output after test additions reported **0** focused warnings for `Stride.Input` in this profile run.
- Warning lines captured by grep filter: `0`.
- Top warning codes: none in captured focused snapshot.
- Comparison to M10c prior count (146 lines): current focused snapshot is lower in this environment/profile; no new focused warning lines were introduced by M10d test additions.

## 8) Validation results
1. `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing upstream build warnings in dependent projects (non-blocking)
   - Result: PASS
   - Output truncated: yes (tool output truncation)
2. `dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no
3. `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no
4. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no
5. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no
6. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: one skipped test (non-failure)
   - Result: PASS
   - Output truncated: no
7. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no
8. `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no
9. Focused snapshot commands:
   - `dotnet build ... | tee /tmp/striv-m10d-input-after-tests.log`
   - `grep ... > /tmp/striv-m10d-input-warning-lines.log || true`
   - `wc -l ...`
   - `sed ... | sort | uniq -c | sort -nr`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: PASS
   - Output truncated: no

## 9) Deferred tests
- SDL runtime lifecycle behavior under real window/event loop.
- Windows RawInput runtime lifecycle behavior.
- Gesture/event pipeline deeper integration scenarios.
- Controller support policy/coverage decisions beyond simulated scope.

## 10) Recommended next task
Proceed with **M10e Shine pass 2** focused on `InputManager` lifecycle/nullability warnings, using these M10d tests as safety rails.
