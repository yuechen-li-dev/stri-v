# M22u CameraProcessor + ModelNodeLinkProcessor cleanup

## 1) Files changed
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/Compositing/SceneCameraSlot.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/ModelNodeLinkComponent.cs
- striv/projects/Stride.Engine/Engine/RenderingLifecycle/ModelNodeTransformLink.cs
- striv/tests/Stride.Engine.Tests/ConcreteProcessorLifecycleTests.cs

## 2) Task scope
Focused concrete processor cleanup around CameraProcessor and ModelNodeLinkProcessor nullability pressure. No processor matching rewrite, no model hierarchy rewrite, no Dominatus migration.

## 3) Before warnings
- Focused warning lines before: 488
- Target lines before included:
  - CameraProcessor CS8625 (multiple sites)
  - ModelNodeLinkProcessor CS8622/CS8625/CS8604/CS8601
  - ModelNodeLinkComponent CS8618
  - ModelNodeTransformLink CS8618

## 4) Classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| CameraProcessor | CS8625 | assigning null into non-nullable slot camera refs | clearing camera attachments is valid | camera slot optional assignment | deferred (requires slot/property nullability alignment beyond targeted files) |
| ModelNodeLinkProcessor event hook | CS8622 | handler sender nullability mismatch | event sender can be null | event/delegate nullability mismatch | fixed by nullable sender signature |
| ModelNodeLinkComponent.Target | CS8618/CS8625 pressure | optional target model before binding | pre-bind inert state is valid | model-node link optional target | fixed via nullable target contract |
| ModelNodeLinkComponent.NodeName | CS8618 | uninitialized non-null string | inert default should be safe | runtime-bound associated data | fixed with empty-string default |
| ModelNodeTransformLink.skeleton | CS8618 | runtime-populated skeleton cache | null until computed is valid | model/transform hierarchy binding lifecycle | fixed with nullable field |
| ModelNodeTransformLink.NeedsRecreate arg | CS8604 callsite | parent entity can be absent | parent-absent should no-op/recreate path | component removal cleanup semantics | fixed via nullable parameter |

## 5) Tests
Added/updated tests in ConcreteProcessorLifecycleTests:
- ModelNodeLinkProcessor_DefaultConstruction_DoesNotRequireRuntimeServices
- ModelNodeLinkComponent_DefaultConstruction_HasValidInertState

## 6) Fixes applied
- ModelNodeLinkComponent: Target made nullable, NodeName defaulted to empty string, ValidityCheck overload accepts nullable target, hierarchy callback sender made nullable.
- ModelNodeTransformLink: skeleton cache made nullable; NeedsRecreate accepts nullable parent entity.
- SceneCameraSlot: Camera property made nullable to reflect detach semantics.

## 7) Deferred issues
- CameraProcessor CS8625/CS8602 cluster remains; requires broader camera-slot lifecycle contract cleanup.
- ModelNodeLinkProcessor retains CS8625/CS8601 sites tied to TransformComponent.TransformLink nullability.
- EntityProcessor associated-data/EntityManager seams unchanged.

## 8) Warning results
- Focused warning lines after: 470
- Total delta: -18
- CameraProcessor bucket: 12 -> 6 (dedup bucket view)
- ModelNodeLinkProcessor CS8622 bucket: 4 -> 0
- No broad public nullable cascade introduced.

## 9) Validation results
See command log in terminal session for exact commands and results. Key outcomes:
- Stride.Engine focused build passed.
- Stride.Engine.Tests passed.
- StriV.Core.slnx build passed.
- Focused-project build script passed.
- Dominatus test suite failed with existing lifecycle attachment assertions.

## 10) Next recommendation
Proceed with a dedicated camera slot lifecycle contract pass (SceneCameraSlotId/TransformLink alignment) before broader EntityManager/default-state cleanup.
