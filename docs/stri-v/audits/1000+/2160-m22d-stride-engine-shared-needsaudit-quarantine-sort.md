# 2160 — M22d Stride.Engine Shared / NeedsAudit / Quarantine sort

## 1) Files changed

### Shared moves
- `Engine/Events/EventKey.cs` -> `Engine/Shared/Events/EventKey.cs`
- `Engine/Events/EventKeyBase.cs` -> `Engine/Shared/Events/EventKeyBase.cs`
- `Engine/Events/EventReceiver.cs` -> `Engine/Shared/Events/EventReceiver.cs`
- `Engine/Events/EventReceiverBase.cs` -> `Engine/Shared/Events/EventReceiverBase.cs`
- `Engine/Events/EventReceiverOptions.cs` -> `Engine/Shared/Events/EventReceiverOptions.cs`
- `Engine/Events/EventTaskScheduler.cs` -> `Engine/Shared/Events/EventTaskScheduler.cs`
- `Engine/Gizmos/GizmoComponentAttribute.cs` -> `Engine/Shared/Gizmos/GizmoComponentAttribute.cs`
- `Engine/Gizmos/IEntityGizmo.cs` -> `Engine/Shared/Gizmos/IEntityGizmo.cs`
- `Engine/Gizmos/IGizmo.cs` -> `Engine/Shared/Gizmos/IGizmo.cs`
- `Engine/Lifecycle/IEntityLifecycleOrchestrator.cs` -> `Engine/Shared/Lifecycle/IEntityLifecycleOrchestrator.cs`
- `Internals/LambdaReadOnlyCollection.cs` -> `Engine/Shared/Internals/LambdaReadOnlyCollection.cs`
- `Engine/Design/DefaultEntityComponentProcessorAttribute.cs` -> `Engine/Shared/Design/DefaultEntityComponentProcessorAttribute.cs`
- `Engine/Design/DefaultEntityComponentRendererAttribute.cs` -> `Engine/Shared/Design/DefaultEntityComponentRendererAttribute.cs`
- `Engine/Design/EffectCompilationMode.cs` -> `Engine/Shared/Design/EffectCompilationMode.cs`
- `Engine/Design/EntityComponentEventArgs.cs` -> `Engine/Shared/Design/EntityComponentEventArgs.cs`
- `Engine/Design/EntityComponentProperty.cs` -> `Engine/Shared/Design/EntityComponentProperty.cs`
- `Engine/Design/EntityComponentPropertyType.cs` -> `Engine/Shared/Design/EntityComponentPropertyType.cs`
- `Engine/Design/ExecutionMode.cs` -> `Engine/Shared/Design/ExecutionMode.cs`
- `Engine/Design/ParameterCollectionResolver.cs` -> `Engine/Shared/Design/ParameterCollectionResolver.cs`
- `Engine/Design/ParameterContainerExtensions.cs` -> `Engine/Shared/Design/ParameterContainerExtensions.cs`

### NeedsAudit moves
- `Engine/Design/EntityChildPropertyResolver.cs` -> `Engine/NeedsAudit/Design/EntityChildPropertyResolver.cs`
- `Engine/FlexibleProcessing/*` -> `Engine/NeedsAudit/FlexibleProcessing/*`

### Quarantine moves
- `Engine/Network/*` -> `Engine/Quarantine/Network/*`
- `Rendering/Compositing/EditorTopLevelCompositor.cs` -> `Engine/Quarantine/RenderingEditor/EditorTopLevelCompositor.cs`

### Project/docs updates
- `striv/projects/Stride.Engine/Stride.Engine.csproj`:
  - `Compile Remove="Rendering/Compositing/EditorTopLevelCompositor.cs"` ->
  - `Compile Remove="Engine/Quarantine/RenderingEditor/EditorTopLevelCompositor.cs"`
- `docs/stri-v/stride-engine-lifecycle-locality-map.md`: appended M22d status section.

## 2) Task scope
Physical organization only: classify/move files into Shared/NeedsAudit/Quarantine folders with namespace preservation and no behavior/API changes.

## 3) Classification doctrine
- Shared: only true cross-cutting contracts/primitives/small utilities.
- NeedsAudit: ownership unclear or manager/factory/stateful abstraction requiring deeper design decision.
- Quarantine: legacy/platform/editor/tooling residue and strategic containment.
- Avoid fake modularity: no file splitting by size; no synthetic modular breakup.
- Ambiguous ownership is not Shared by default.

## 4) Shared moves
| Old path | New path | Why Shared | Confidence |
|---|---|---|---|
| `Engine/Events/*` | `Engine/Shared/Events/*` | cross-cutting event primitives/contracts | Medium |
| `Engine/Gizmos/*` | `Engine/Shared/Gizmos/*` | shared gizmo contracts/metadata | Medium |
| `Engine/Lifecycle/IEntityLifecycleOrchestrator.cs` | `Engine/Shared/Lifecycle/IEntityLifecycleOrchestrator.cs` | cross-lifecycle orchestrator contract seam | High |
| `Internals/LambdaReadOnlyCollection.cs` | `Engine/Shared/Internals/LambdaReadOnlyCollection.cs` | generic utility collection helper | Medium |
| selected `Engine/Design/*` metadata/attribute helpers | `Engine/Shared/Design/*` | reusable metadata/contracts used across lifecycle folders | Medium |

## 5) NeedsAudit moves
| Old path | New path | Why NeedsAudit | Future decision needed |
|---|---|---|---|
| `Engine/FlexibleProcessing/*` | `Engine/NeedsAudit/FlexibleProcessing/*` | manager/processor abstractions may hide lifecycle policy/state | localize vs formal shared contract vs Dominatus policy surface |
| `Engine/Design/EntityChildPropertyResolver.cs` | `Engine/NeedsAudit/Design/EntityChildPropertyResolver.cs` | unclear ownership and policy locality | resolve lifecycle owner (Entity/Scene/Serialization boundary) |

## 6) Quarantine moves
| Old path | New path | Compile-included? | Why Quarantine | Notes |
|---|---|---:|---|---|
| `Engine/Network/*` | `Engine/Quarantine/Network/*` | Yes | strategic containment of legacy/platform socket/router stack | intentionally still included; no new `Compile Remove` entry |
| `Rendering/Compositing/EditorTopLevelCompositor.cs` | `Engine/Quarantine/RenderingEditor/EditorTopLevelCompositor.cs` | No | editor-only compositor glue outside clean runtime graph | `Compile Remove` path updated |

## 7) Files intentionally left unmoved
- `Properties/AssemblyInfo.cs`: left in place due assembly-level build conventions and `InternalsVisibleTo` scope.

## 8) Project/include behavior
- Wildcard compile (`**/*.cs`) kept included files compiling after moves.
- Only `Compile Remove` update required was EditorTopLevelCompositor moved path.
- Network remained compile-included after quarantine move (strategic folder placement only).

## 9) Namespace/API preservation
- Namespaces unchanged.
- Public API unchanged.
- No file splits/type renames/partial-class creation.

## 10) Warning/path result
- Focused warning lines capture generated at `/tmp/striv-m22d-engine-warning-lines.log`.
- Count: see `wc -l` output from validation section.
- Paths reflect new Shared/NeedsAudit/Quarantine locations where files moved.

## 11) Validation results
| Command | Exit code | First meaningful warning/error | Pass/fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -v minimal` | 0 | warning-only baseline in dependent projects | Pass | Yes (terminal capture truncated) |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m22d-engine-build.log` | 0 | `CompressedTimeSpan.cs` nullability warnings | Pass | No (logged) |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal 2>&1 | tee /tmp/striv-m22d-engine-tests.log` | 0 | none | Pass | No (logged) |

## 12) Next task recommendation
Resume nullability cleanup folder-by-folder, starting with `DiagnosticsProfilingLifecycle/GameProfilingSystem` path after move topology stabilization, then run a dedicated `NeedsAudit/FlexibleProcessing` doctrine pass.
