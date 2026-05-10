# M22t Entity concrete processor cleanup

## 1) Files changed
- striv/projects/Stride.Engine/Engine/EntityLifecycle/Processors/CameraProcessor.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/Processors/InstancingProcessor.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/Processors/LightShaftProcessor.cs
- striv/projects/Stride.Engine/Engine/EntityLifecycle/Processors/LightShaftBoundingVolumeProcessor.cs
- striv/tests/Stride.Engine.Tests/ConcreteProcessorLifecycleTests.cs
- docs/stri-v/audits/1000+/2310-m22t-entity-concrete-processor-cleanup.md

## 2) Task scope
Focused on concrete EntityLifecycle processors and safe nullability contract cleanup. No matching rewrite, no required-type policy rewrite, no Dominatus migration.

## 3) Before warnings
- Focused warning lines before: 522
- Notable processor buckets before:
  - LightShaftBoundingVolumeProcessor CS8622: 12
  - CameraProcessor CS8625: 12
  - InstancingProcessor CS8618: 10
  - LightShaftProcessor CS8618: 6

## 4) Processor warning classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| CameraProcessor | CS8618 | runtime compositor field | compositor absent before draw is valid | unbound lifecycle state | made field nullable |
| LightShaftBoundingVolumeProcessor | CS8622 | event sender signature mismatch | sender can be null for EventHandler | processor event delegate nullability mismatch | changed handlers to object? |
| InstancingProcessor.InstancingData | CS8618 | runtime-populated associated data fields | data is populated during add path | runtime-initialized associated data | made fields nullable and guarded usage |
| InstancingProcessor | CS8618 | runtime-bound render processor | resolved on system add | unbound lifecycle state | nullable field + guarded access |
| LightShaftProcessor.AssociatedData | CS8618 | runtime-populated associated data | component/light optional during lifecycle checks | optional component relationship | nullable data fields + guards |
| LightShaftBoundingVolumeProcessor | CS8603/CS8600 | optional lookup value | no volumes for component is valid | optional component relationship | nullable return + out var cleanup |

## 5) Tests
Added constructor/inert-state tests:
- CameraProcessor_DefaultConstruction_DoesNotRequireRuntimeServices
- InstancingProcessor_DefaultConstruction_HasValidInertState
- LightShaftProcessor_DefaultConstruction_DoesNotRequireRuntimeServices
- LightShaftBoundingVolumeProcessor_DefaultConstruction_DoesNotRequireRuntimeServices

## 6) Fixes applied
- CameraProcessor: made current compositor field nullable and callback sender nullable.
- InstancingProcessor: nullable runtime-bound members in InstancingData, nullable modelRenderProcessor field, guarded map usage, lifecycle-safe visibility tag removal.
- LightShaftProcessor: lifecycle-safe visibility group backing seam, nullable associated light/component fields, guard before dereference.
- LightShaftBoundingVolumeProcessor: fixed EventHandler sender nullability, nullable lookup return, and safer collection initialization pattern.

## 7) Warning results
- Focused warning lines after: 488 (delta -34)
- Concrete processor deltas:
  - LightShaftBoundingVolumeProcessor CS8622 removed (12 -> 0)
  - InstancingProcessor CS8618 removed (10 -> 0)
  - LightShaftProcessor CS8618 removed (6 -> 0)
  - CameraProcessor CS8618 removed; CS8625 remains at 12 (deferred)
- Public nullable cascade avoided for EntityProcessor membership contracts.

## 8) Deferred issues
- EntityProcessor<TComponent,TData> CS8604 edges (matching/remove associated-data contract)
- EntityManager constructor/default policy seam cleanup
- Required-type dependency lifecycle policy
- Remaining CameraProcessor CS8625 slot null-assignment contract
- Dominatus lifecycle modeling not touched

## 9) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` exit 0, pass, warnings present, output truncated: yes.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` exit 0, pass, warnings present from build, output truncated: yes.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` (after) exit 0, pass, warnings present, output truncated: yes.

## 10) Next recommendation
Continue concrete processor cleanup with CameraProcessor CS8625 slot contract and then ModelNodeLinkProcessor CS8622/CS8625, before entering broader EntityManager constructor/default-state cleanup.
