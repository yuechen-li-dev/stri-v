# 2500 — M23n Light registration actuator boundary

## 1. Files changed
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Actuation/ILightRegistrationActuator.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Lights/LightProcessor.cs
- striv/tests/Stride.Engine.Tests/RenderingLightLifecycleTests.cs

## 2. Task scope
Implemented an internal light-registration actuator seam in `Stride.Engine` without processor matching rewrites, lighting math changes, render pipeline rewrites, or Dominatus dependency import.

## 3. Dominatus actuation reference
- Attempted reads of `striv/external/Dominatus/docs/ARCHITECTURE.md` and `AUTHORING_GUIDE.md` (paths absent in this checkout).
- Reviewed actuation shape by scanning Dominatus runtime/adapters/tests using `rg`.
- Applied lessons: typed narrow actuator methods with concrete payloads and explicit side-effect naming.
- Imported no Dominatus dependency into `Stride.Engine`.

## 4. Responsibility split
| LightProcessor responsibility | Category | Kept in processor? | Moved behind actuator? |
| --- | --- | ---: | ---: |
| Component matching / `EntityProcessor` membership | query/membership | yes | no |
| Enabled/type/update-gating in draw loop | policy/decision | yes | no |
| Mapping component state to `RenderLight` mutable fields | side effect/actuation | no | yes (`UpdateLight`) |
| Component-register/remove side-effect seam | side effect/actuation | no | yes (`RegisterLight`/`UnregisterLight`) |
| Visibility tag binding in `OnSystemAdd/OnSystemRemove` | service/render bridge | yes | no |
| Per-frame `Lights` collection clear/add | render resource lifecycle | yes | no |

## 5. Actuator interface design
Interface: `ILightRegistrationActuator` (internal).
Methods:
- `RegisterLight(LightComponent component, RenderLight renderLight)`
- `UpdateLight(LightComponent component, RenderLight renderLight)`
- `UnregisterLight(LightComponent component)`

Payloads are concrete engine/render types, with no generic bus or object payloads.

## 6. Tests
Added/updated in `RenderingLightLifecycleTests`:
- `LightRegistrationActuator_RegisterAndUnregisterLight_UpdatesRenderLightLookup`
- `LightRegistrationActuator_UnregisterMissingLight_DoesNotThrow`

## 7. Implementation details
- `LightProcessor` now implements `ILightRegistrationActuator`.
- `OnEntityComponentAdding` routes through `RegisterLight`.
- `OnEntityComponentRemoved` routes through `UnregisterLight`.
- Draw-loop mutable mapping of component to render-light state routes through `UpdateLight`.
- Ordering/policy remains unchanged.

## 8. Dominatus dependency boundary
`rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj'`
- Result: no matches.

## 9. Warning results
- Focused warning count before: 274
- Focused warning count after: 274
- Light warning buckets: no new light-processor warning deltas observed.

## 10. Deferred issues
- Light enabled/type policy remains in legacy `LightProcessor`.
- Processor remains render-bridge shell.
- Probe/light broader render lifecycle unchanged.
- Future Dominatus adapter can target this typed actuator seam.
- Additional actuator candidates remain (light probes, broader render registration, script/action seams).

## 11. Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` — exit 0 — pass.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` — exit 0 — pass.
- Warning extraction/bucketing/grep commands for before/after logs — exit 0 — pass.
- Dominatus-boundary grep command — exit 0 — pass.
- Part 11 full matrix not executed in this pass.

## 12. Next recommendation
Next pilot: **LightProbeRegistration actuator** for parallel render-lifecycle seam extraction while keeping processor query/policy shells intact.
