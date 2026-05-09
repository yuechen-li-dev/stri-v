# StriV.Engine.Dominatus.Runtime

`StriV.Engine.Dominatus.Runtime` is an **opt-in** runtime host for Dominatus-backed Stri-V engine lifecycle scripts.

## Scope

- Lives outside `Stride.Engine`.
- Does **not** rewire or replace the default engine runtime loop.
- Uses lifecycle vocabulary from `StriV.Engine.Dominatus`.
- Uses production side-effect wrappers from `StriV.Engine.Dominatus.Adapters`.

## Current capability (M20b)

- `StriVEngineLifecycleRunner` remains the low-level Dominatus runtime executor.
- `IEntityLifecycleOrchestrator` defines a callsite-facing lifecycle seam for future opt-in consumers.
- `DominatusEntityLifecycleOrchestrator` is the current Dominatus-backed implementation and delegates to `StriVEngineLifecycleRunner`.
- The interface surface uses Stride lifecycle types and does not expose Dominatus runtime internals.

## Integration direction

- This seam is intentionally outside `Stride.Engine` in M20b.
- Future callsites should depend on `IEntityLifecycleOrchestrator` (or a later engine-owned neutral seam), not Dominatus internals.
- No default runtime behavior changes are introduced by this package.
