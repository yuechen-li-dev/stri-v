# M22y — Stride.Engine finishing sweep wave 4

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/DiagnosticsProfilingLifecycle/GameProfilingSystem.cs`
- `striv/projects/Stride.Engine/Engine/DiagnosticsProfilingLifecycle/DebugTextSystem.cs`
- `striv/projects/Stride.Engine/Engine/GameLifecycle/GameSystem.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Skyboxes/CubemapRendererBase.cs`
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/Skyboxes/CubemapSceneRenderer.cs`
- `striv/projects/Stride.Engine/Engine/Shared/Events/EventReceiver.cs`
- `striv/projects/Stride.Engine/Engine/Shared/Events/EventReceiverBase.cs`
- `striv/projects/Stride.Engine/Engine/Shared/Events/EventTaskScheduler.cs`
- `docs/stri-v/audits/1000+/2360-m22y-stride-engine-finishing-sweep-4.md`

## 2) Task scope
Wave 4 finishing sweep was executed with network deletion verification first, followed by small/local warning cleanup in diagnostics/events/game/render buckets. No suppression, no rewrite, no Dominatus migration, and policy-heavy buckets (STRIDE2000 / UpdateEngine / processor matching / deep render invariants) were deferred.

## 3) Network deletion result
- `Engine/Quarantine/Network` is absent.
- Remaining symbol references were found in quarantine shader-compiler files only.
- Focused `Stride.Engine` build still passed after deletion.
- No stubs were needed in this pass.

## 4) Before warnings
- Focused warning count before: **372**
- Top buckets before included:
  - `UpdateEngine.cs` CS8600/CS8604
  - `EntityManager.cs` CS8618/CS8604
  - `GameProfilingSystem.cs` CS8602
  - `Entity.cs` CS8618/CS8603
  - `EntityComponentCollection.cs` CS8625

## 5) Classification table
| Bucket | Warning | File(s) | Category | Action |
|---|---|---|---|---|
| Network deletion | compile refs only | Quarantine/Shaders.Compiler/* | network deletion fallout | Verified build pass, no stubs |
| Diagnostics | CS8602/CS8629 | GameProfilingSystem, DebugTextSystem | diagnostics display/runtime state | local guards and safer access patterns |
| Events | CS8601/CS8603/CS8625 | EventReceiverBase, EventReceiver, EventTaskScheduler | event/delegate nullability | inert default values / empty enumerable |
| Game lifecycle | CS8600/CS8603 | GameSystem | game lifecycle optional service | made `Game` nullable |
| Skybox rendering | CS8602/CS8604 | CubemapSceneRenderer, CubemapRendererBase | rendering optional component state | local null-safe compositor restore and depth arg handling |
| Defer | STRIDE2000, UpdateEngine | multiple | policy-heavy defer | deferred unchanged |

## 6) Tests
No new tests added in this pass. Changes were local nullability/default-state adjustments with no intended behavioral expansion.

## 7) Fixes applied
- Replaced unsafe event name sort access with null-safe fallback names in profiling sorting.
- Removed forced null-forgiving in present interval capture.
- Added debug font load guard in `DebugTextSystem` before renderer initialization.
- Returned an empty task enumerable in `EventTaskScheduler.GetScheduledTasks()`.
- Used explicit null-forgiving only at internal handoff points in event receiver internals.
- Made `GameSystem.Game` nullable to match documented/actual mock usage.
- Restored skybox compositor with fallback when original game compositor is null; reduced nullable warnings in assignment flow.
- Applied nullable-safe argument flow in cubemap depth-stencil binding.

## 8) Deferred issues
- STRIDE2000 warnings.
- `UpdateEngine` runtime invariants and nullable flow.
- Processor matching / required-type policy paths.
- Deep GPU/render lifecycle invariants.
- Network replacement strategy remains future work (dedicated module).

## 9) Warning results
- Focused warning count after: **354**
- Delta: **-18**
- Reduced/cleared buckets in this pass mostly from events/game-local nullability and small diagnostics/render adjustments.
- Remaining top buckets still led by UpdateEngine/EntityManager/policy-heavy families.

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, warnings present, pass, output truncated: no (saved to log).
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -v minimal` → exit 0, warnings present, pass, output truncated: no.
- Network deletion/reference checks commands → exit 0, pass.

## 11) Next recommendation
Proceed with **finishing sweep 5** focused on additional safe local buckets, while continuing to defer STRIDE2000 and UpdateEngine policy-heavy invariants.
