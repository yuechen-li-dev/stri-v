# M23m — TransformHierarchy actuator-boundary pilot

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/Actuation/ITransformHierarchyActuator.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/TransformComponent.cs`
- `striv/tests/Stride.Engine.Tests/TransformHierarchyActuatorTests.cs`

## 2) Task scope
Implemented a conservative TransformHierarchy actuator-boundary pilot that isolates parent/child hierarchy side effects behind typed methods while preserving the existing transform propagation, scene-root semantics, and processor ordering. No Dominatus references were introduced.

## 3) Dominatus actuation reference
The requested Dominatus docs paths were not present at `striv/external/Dominatus/docs/...`; actuation-shape guidance was instead read from existing Dominatus runtime/tests via pattern search (`/tmp/striv-m23m-dominatus-actuation-shape-search.txt`). Key lessons applied: explicit typed methods, deterministic invalid-state behavior via existing Stride guards, and side-effect seams separated from policy.

## 4) Responsibility split
| Transform responsibility | Category | Kept in current code? | Moved behind actuator? |
| --- | --- | ---: | ---: |
| `Position/Rotation/Scale/WorldMatrix/LocalMatrix` storage | data storage | Yes | No |
| Parent-child attach/detach/reparent side effects | hierarchy membership / side effect | Yes | Yes (pilot seam) |
| World/local matrix updates | propagation policy | Yes | No |
| Root-scene movement flag and manager notifications | runtime update state | Yes | Indirect (unchanged behavior) |
| `TransformLink` / model node bridge invocation | model-node link bridge | Yes | Already handled by prior M23l seam |

## 5) Actuator interface design
- Interface: `ITransformHierarchyActuator`
- Methods:
  - `AttachParent(TransformComponent child, TransformComponent parent)`
  - `DetachParent(TransformComponent child)`
- Payloads use concrete engine types; no object payloads.
- No generic actuator bus: this pilot intentionally keeps a narrow, named side-effect seam.

## 6) Tests
Added dedicated tests:
- `TransformHierarchyActuator_AttachParent_UpdatesParentAndChildren`
- `TransformHierarchyActuator_DetachParent_ClearsParentAndChildCollection`
- `TransformHierarchyActuator_Reparent_UpdatesOldAndNewParentCollections`
- `TransformComponent_ParentSetter_StillMaintainsHierarchy`

## 7) Implementation details
- Added internal `TransformHierarchyActuator` in `TransformComponent` implementing the new interface.
- Routed `TransformComponent.Parent` setter add/remove side effects through actuator calls.
- Reparent path in actuator detaches from old parent before attaching to new parent.
- Left transform propagation and scene-root policy logic unchanged.

## 8) Processor checklist
| Processor/system | Current status | Actuator boundary exists? | Next required seam | Priority | Notes |
| --- | --- | ---: | --- | --- | --- |
| CameraProcessor | actuator extracted | Yes | camera slot policy extraction | Medium | M23k done |
| ModelNodeLinkProcessor | partial actuator extracted | Yes | membership/query split completion | Medium | M23l done |
| TransformProcessor | partial actuator extracted | Yes | processor-owned root/manager side effects | High | M23m pilot |
| ModelTransformProcessor | needs design | No | transform-operation registration actuator | Medium | keep propagation intact |
| ModelRenderProcessor | render bridge keep | No | render-registration seam (if needed) | High | render bridge retained |
| SpriteRenderProcessor | render bridge keep | No | render-registration seam | Medium | |
| BackgroundRenderProcessor | render bridge keep | No | render-registration seam | Medium | |
| LightProcessor | render bridge keep | No | light registration actuator | High | |
| LightProbeProcessor | render bridge keep | No | probe registration actuator | High | |
| InstancingProcessor | render bridge keep | No | instancing registration seam | Medium | |
| InstanceProcessor | data-layer helper keep | No | none immediate | Low | |
| AnimationProcessor | Dominatus replacement candidate | No | animation actuation seam | Medium | |
| SpriteAnimationSystem | Dominatus replacement candidate | No | frame-step seam | Medium | |
| ScriptSystem | Dominatus replacement candidate | No | script scheduling/action seam | High | |
| ScriptProcessor | Dominatus replacement candidate | No | script component lifecycle seam | High | |
| DebugTextSystem | diagnostics keep | No | none | Low | |
| GameProfilingSystem | diagnostics keep | No | none | Low | |
| SceneSystem | needs design | No | scene instance/render handoff seam | Medium | |
| AudioEmitterProcessor (Quarantine) | delete/quarantine candidate | No | quarantine or remove | Low | currently in Quarantine |
| AudioListenerProcessor (Quarantine) | delete/quarantine candidate | No | quarantine or remove | Low | currently in Quarantine |

## 9) Dominatus dependency boundary
`rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj'`
- Result: no matches.

## 10) Warning results
- Focused warnings before: `274`
- Focused warnings after: `274`
- Transform-related buckets: effectively unchanged (expected for behavior-preserving actuator seam).

## 11) Deferred issues
- Transform propagation policy extraction (explicitly deferred)
- Scene root membership actuation (deferred)
- Render registration actuators (deferred)
- Light/probe registration actuators (deferred)
- Script/action replacement seam (deferred)

## 12) Validation results
Commands were executed as requested; all completed with exit code 0 in the final run. Early iteration had one failing reparent test, fixed by detach-before-attach in actuator. Full logs are in terminal output and generated artifacts.

## 13) Next recommendation
**LightRegistration actuator pilot** (high leverage with render bridge retained, lower risk than transform-policy extraction).
