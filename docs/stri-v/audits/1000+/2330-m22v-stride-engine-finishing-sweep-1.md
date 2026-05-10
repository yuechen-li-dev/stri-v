# M22v Stride.Engine finishing sweep 1

## 1) Files changed
- Engine: CameraProcessor, EventReceiverBase, GameSettings, LightShaftBoundingVolumeComponent, ModelComponent.
- Tests: RenderingLifecycleConstructionTests, GameLifecycleTests.

## 2) Task scope
Finishing sweep focused on nullable warnings with camera slot lifecycle priority and low-risk constructor/default-state buckets. No STRIDE2000 work attempted. No processor matching rewrite, no UpdateEngine architecture rewrite, no render pipeline rewrite.

## 3) Before warnings
- Focused warning count before: 470
- Top buckets before included UpdateEngine CS8600, EntityManager CS8618/CS8604, EventReceiverBase CS8618, LightShaftBoundingVolumeComponent CS8618, GameSettings CS8618, CameraProcessor CS8625.

## 4) Classification table
| Bucket | Warning | File(s) | Category | Action |
| --- | --- | --- | --- | --- |
| Camera slot detach contract | CS8625 | CameraProcessor | camera slot optional detach contract | Partially reviewed, kept semantics; only safe diagnostic-string guard change |
| Event receiver state | CS8618/CS8600/CS8601 | EventReceiverBase | event receiver optional state | Initialize inert defaults and nullable private seams |
| Settings defaults | CS8618 | GameSettings | constructor/default collection state | Initialize string defaults and platform config |
| Light shaft volume defaults | CS8618 | LightShaftBoundingVolumeComponent | render optional component state | mark optional model/shaft references nullable |
| Model component defaults | CS8618 | ModelComponent | render optional component state | nullable-safe field initialization without interface contract change |
| UpdateEngine runtime navigation | CS8600/CS8604/CS8601 | UpdateEngine | update-engine runtime invariant | deferred |
| STRIDE2000 | STRIDE2000 | ParameterCollectionResolver/EntityChildPropertyResolver | obsolete/internal deferred | deferred |

## 5) Tests
- Added SceneCameraSlot default-inert test.
- Added ModelComponent default-inert test.
- Added LightShaftBoundingVolumeComponent default-inert test.
- Added GameSettings default defaults test.

## 6) Fixes applied
- EventReceiverBase: switched lifecycle-private references to nullable/initialized defaults and tightened out-var patterns.
- GameSettings: provided explicit empty-string and configuration defaults.
- LightShaftBoundingVolumeComponent: optional runtime links now nullable.
- ModelComponent: initialized runtime fields safely while preserving `IModelInstance.Model` non-nullable signature.
- CameraProcessor: guarded detach error message when slot camera is absent.

## 7) Deferred issues
- STRIDE2000 buckets.
- UpdateEngine runtime navigation/nullability invariants.
- Processor matching/required-type policy buckets.
- GPU/render lifecycle deeper invariants.
- Remaining CameraProcessor CS8625 nullability seams tied to slot/compositor attach lifecycle contracts.

## 8) Warning results
- Focused warning count after: 438
- Delta: -32
- Reduced several constructor/default-state warnings, but camera bucket remains policy-shaped.

## 9) Validation results
- See command transcript from this pass; major checks executed include focused build before/after and Stride.Engine.Tests.

## 10) Next recommendation
Finish sweep 2 for remaining low-risk constructor/default buckets, then perform isolated STRIDE2000 pass.
