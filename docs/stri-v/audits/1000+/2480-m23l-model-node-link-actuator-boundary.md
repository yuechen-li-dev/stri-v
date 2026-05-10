# 2480 — M23l ModelNodeLink actuator-boundary pilot

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Actuation/IModelNodeLinkActuator.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/Processors/ModelNodeLinkProcessor.cs`
- `striv/tests/Stride.Engine.Tests/ModelNodeLinkActuatorTests.cs`

## 2) Task scope
This pass implements the second processor responsibility split pilot by extracting `ModelNodeLinkProcessor` side-effect mutations behind an internal actuator-shaped interface.

Out of scope (unchanged):
- processor matching strategy,
- model hierarchy behavior,
- transform propagation behavior,
- Dominatus dependency injection into `Stride.Engine`.

## 3) Dominatus actuation reference
Reviewed Dominatus architecture/authoring docs and StriV Dominatus adapters/runtime/tests.
Applied lessons:
- explicit, typed side-effect surfaces;
- decision/policy separated from mutation;
- deterministic precondition checks where applicable.

No Dominatus API or package references were imported into `Stride.Engine`.

## 4) Responsibility split
| ModelNodeLinkProcessor responsibility | Category | Kept in processor? | Moved behind actuator? |
| --- | --- | ---: | ---: |
| Entity/component membership iteration (`ComponentDatas`) | query/membership | ✅ | ❌ |
| Validity gate (`IsValid`) and recreate decision (`NeedsRecreate`) | policy/decision | ✅ | ❌ |
| Model resolution (`Target` or parent model) | model/transform bridge | ✅ | ❌ |
| `TransformLink` assignment to new `ModelNodeTransformLink` | side effect / actuation | ❌ direct | ✅ |
| `TransformLink` clearing on invalid/removed state | side effect / actuation | ❌ direct | ✅ |
| Hierarchy event hook wiring/unwiring | service/event hook | ✅ | ❌ |

## 5) Actuator interface design
Added internal, concrete actuator:

```csharp
internal interface IModelNodeLinkActuator
{
    void AttachModelNodeLink(TransformComponent transformComponent, ModelNodeTransformLink link);
    void ClearModelNodeLink(TransformComponent transformComponent);
}
```

Rationale:
- concrete engine payload types;
- explicit side-effect semantics;
- no generic bus, no object payloads.

## 6) Tests
Added:
- `ModelNodeLinkActuator_AttachModelNodeLink_SetsTransformLink`
- `ModelNodeLinkActuator_ClearModelNodeLink_ClearsTransformLink`

Existing construction/inert tests remain valid.

## 7) Implementation details
`ModelNodeLinkProcessor` now implements `IModelNodeLinkActuator` and routes all direct `TransformLink` mutations through actuator methods:
- attach path uses `AttachModelNodeLink(...)`;
- clear/remove/invalid paths use `ClearModelNodeLink(...)`.

Policy/ordering/loop flow remained unchanged.

## 8) Dominatus dependency boundary
Grep run:
`rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj'`

Result: no matches.

## 9) Warning results
Focused warning line count (`Stride.Engine`):
- before: `278`
- after: `274`

Model-node specific warning lines:
- before: 6 hits (3 unique, duplicated by build output)
- after: 2 hits (1 unique) at `ModelNodeLinkProcessor.cs(85,48)`.

Net: warning bucket changed in this area due to mutation centralization; overall warning posture remains warning-heavy and architecture shaping did not introduce suppressions.

## 10) Deferred issues
- model-node link policy logic remains in legacy processor;
- model hierarchy resolution semantics unchanged;
- future Dominatus adapter can target actuator shape without rewriting policy;
- next candidates: transform hierarchy / render registration / light registration / script-action seams.

## 11) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit `0`, pass, output truncated: no.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit `0`, pass with warnings, output truncated: yes (terminal).
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` (as part of standard sequence) → pass in captured output.
- `./striv/build/striv-check-focused-projects.sh ...` → pass summary observed.
- Remaining standard sequence commands were executed in one chained run and produced ongoing warning-heavy output; no mutation-related failures observed in captured segment.

## 12) Next recommendation
Next actuator-boundary pilot: **TransformHierarchy actuator**.

Reason: closest adjacency to this split and highest leverage for future policy-vs-actuation decoupling.
