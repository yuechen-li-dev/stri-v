# M22s — EntityLifecycle processor binding / services contract cleanup (narrowed, test-first)

## 1) Files changed
- `striv/tests/Stride.Engine.Tests/EntityLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2310-m22s-entity-processor-binding-contract-cleanup.md`

## 2) Task scope
- Narrowed pass on processor binding/services lifecycle contract verification.
- No processor matching rewrite.
- No required-type dependency behavior rewrite.
- No Dominatus migration.

## 3) Before warnings
- Focused warnings (Stride.Engine scoped lines): **522**.
- EntityLifecycle warning lines: **164**.
- Top relevant buckets (sample):
  - `Engine/EntityLifecycle/EntityManager.cs CS8618` (12)
  - `Engine/EntityLifecycle/Processors/CameraProcessor.cs CS8625` (12)
  - `Engine/EntityLifecycle/Processors/LightShaftBoundingVolumeProcessor.cs CS8622` (12)
  - `Engine/EntityLifecycle/EntityProcessor.cs CS8604` (4)

## 4) Processor lifecycle classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
|---|---|---|---|---|---|
| `EntityProcessor.EntityManager` | guard path (no current warning) | pre-bind/post-remove null internal state | public access must throw deterministic invalid operation when unbound | public guarded contract | validated via tests |
| `EntityProcessor.Services` | guard path (no current warning) | services unavailable before add / after remove | protected access must throw deterministic invalid operation when unavailable | public/protected guarded contract | validated via tests |
| `EntityManager.OnProcessorAdded/Removed` | none new | bind manager/services on add; clear on remove | lifecycle transition consistency | processor collection ownership | validated via tests |
| `EntityProcessor<TComponent,TData>` callsites | CS8604 | matching/remove data nullability edges | tied to matching and associated data policy | required-type dependency invariant | deferred (policy-sensitive) |

## 5) Tests
Added tests in `EntityLifecycleTests`:
- `EntityProcessor_BoundLifecycleAccess_ReturnsManagerAndServices`
- `EntityProcessor_RemovedLifecycleAccess_ThrowsInvalidOperationException`

Pre-existing and retained:
- `EntityProcessor_UnboundLifecycleAccess_ThrowsInvalidOperationException`

These cover unbound/bound/removed lifecycle access semantics deterministically.

## 6) Fixes applied
- No source contract change was required in `EntityProcessor`/`EntityManager`; current backing-field guarded pattern from M22r/M22r-hotfix is already correct.
- Added test coverage to lock behavior and prevent regressions around processor binding/removal service-manager contract use.

## 7) Warning results
- Focused warnings after: **522**.
- EntityLifecycle warning lines after: **164**.
- Delta: **0** (no warning cascade, no regression).
- No newly exposed warnings from this pass.

## 8) Deferred issues
- Processor matching invariants in `EntityProcessor<TComponent,TData>` (CS8604 edges).
- Required type dependency lifecycle policy in `EntityManager`/processor maps.
- Broader EntityManager nullable-policy seams outside processor binding contract.
- Future Dominatus lifecycle modeling (out of scope for this pass).

## 9) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none | pass | no |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` | 0 | existing focused warning baseline unchanged | pass | no |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | existing warnings in other projects/tests | pass | yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | pass | no |

## 10) Next recommendation
Continue narrowed **EntityLifecycle processor-policy pass** next, specifically around `EntityProcessor<TComponent,TData>` CS8604 warning sites where behavior can be formalized without changing matching semantics.
