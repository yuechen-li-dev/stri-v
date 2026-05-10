# M23k — Camera slot actuator boundary pilot

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Actuation/ICameraSlotActuator.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/Processors/CameraProcessor.cs`
- `striv/tests/Stride.Engine.Tests/CameraSlotActuatorTests.cs`
- `docs/stri-v/audits/1000+/2470-m23k-camera-slot-actuator-boundary.md`

## 2) Task scope
This pass pilots an internal actuator boundary for camera slot mutation in `CameraProcessor`.
- No processor matching rewrite performed.
- No render pipeline/compositor semantics rewrite performed.
- No Dominatus dependency added to `Stride.Engine`.

## 3) Responsibility split
| CameraProcessor responsibility | Category | Kept in processor? | Moved behind actuator? |
| --- | --- | ---: | ---: |
| `EntityProcessor<CameraComponent>` membership loop | query/membership | Yes | No |
| enabled/disabled attach-detach decision flow | policy/decision | Yes | No |
| compositor change/slot dirty bookkeeping | runtime state | Yes | No |
| `slot.Camera = camera` side effect | side effect/actuation | No (direct) | Yes |
| `slot.Camera = null` side effect | side effect/actuation | No (direct) | Yes |
| search slot by id/conflict checks | render/compositor bridge + policy | Yes | No |

## 4) Actuator interface design
- Interface: `ICameraSlotActuator` (internal).
- Methods:
  - `AttachCamera(SceneCameraSlot slot, CameraComponent camera)`
  - `ClearCamera(SceneCameraSlot slot)`
- Payloads are concrete engine types to keep call sites explicit and strongly typed.
- No generic event bus/object payload pattern added.
- First pilot keeps implementation in legacy processor (`CameraProcessor : ICameraSlotActuator`) with explicit boundary methods.

## 5) Tests
Added:
- `CameraSlotActuator_AttachAndClearCamera_UpdatesSlotCamera`
  - verifies actuator attach sets `slot.Camera`
  - verifies clear resets `slot.Camera` to null

Retained:
- `CameraProcessor_DefaultConstruction_DoesNotRequireRuntimeServices`

## 6) Implementation details
- Introduced explicit internal actuator methods on `CameraProcessor` and routed slot camera writes through them.
- Existing lifecycle/policy code paths (`AttachCameraToSlot`, detach helpers, dirty-slot cleanup) now call `AttachCamera(...)` / `ClearCamera(...)` instead of mutating `slot.Camera` directly.
- Ordering and behavior preserved; only the mutation boundary changed.

## 7) Dominatus dependency boundary
Command:
`rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`

Result: no matches.

## 8) Warning results
Focused warning lines (`Stride.Engine`) before/after:
- before: 278
- after: 278

Camera-related focused lines:
- before: `CameraProcessor` warnings at lines 59, 169, 182 (duplicated per target framework pass)
- after: `CameraProcessor` warnings at lines 59, 190, 203 (same count, shifted line numbers)

Net: warning count unchanged.

## 9) Deferred issues
- Camera slot policy and slot-id resolution remain in legacy processor.
- Future Dominatus adapter can target the actuator-shaped methods directly.
- Next candidate processors for actuator-boundary extraction:
  - ModelNodeLink
  - TransformHierarchy
  - RenderRegistration
  - LightRegistration

## 10) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m23k-engine-before.log` | 0 | existing nullable warnings (e.g. `GraphicsCompositorHelper.cs` CS8625) | Pass | Yes |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` (pre-edit) | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` (post-edit) | 0 | none | Pass | Yes |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m23k-engine-after.log` | 0 | existing nullable warnings | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | existing warnings in other projects (e.g. CS1030 in assembly processor sources) | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | existing CS0618 test warnings | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | Yes |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | Yes |

## 11) Next recommendation
Next actuator-boundary pilot: **ModelNodeLink actuator**.
Reason: its side effects (link/unlink registration operations) are narrower than transform hierarchy and lower risk than render/light registration.
