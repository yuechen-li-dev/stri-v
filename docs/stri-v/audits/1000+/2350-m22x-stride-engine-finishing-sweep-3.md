# 2350 — M22x Stride.Engine finishing sweep wave 3

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/GameLifecycle/InputSystem.cs`
- `striv/projects/Stride.Engine/Engine/DiagnosticsProfilingLifecycle/DebugTextSystem.cs`
- `striv/projects/Stride.Engine/Engine/DiagnosticsProfilingLifecycle/GameProfilingSystem.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityComponentCollection.cs`
- `docs/stri-v/audits/1000+/2350-m22x-stride-engine-finishing-sweep-3.md`

## 2) Task scope
Wave-3 finishing sweep targeted safe local nullable-warning cleanup in InputSystem, diagnostics/profiling, entity local default-state semantics, and rendering default-state seams. Deferred STRIDE2000, UpdateEngine runtime invariants, processor-policy areas, Quarantine/Network fire-and-forget, and deep render-pipeline invariants.

## 3) Before warnings
- Focused warning count before: **414**
- Top buckets before:
  - `CS8618` 74
  - `CS8625` 68
  - `CS8602` 62
  - `CS8601` 46
  - `CS8603` 38

## 4) Classification table
| Bucket | Warning | File(s) | Category | Action |
|---|---|---|---|---|
| Input delegates | CS8622 | InputSystem.cs | event/delegate nullability mismatch | changed handlers to `object? sender` |
| Debug text draw | CS8602/CS8625 | DebugTextSystem.cs | diagnostics display/runtime resource | added local game guard; null-forgiving for null depth target |
| Profiling draw/enable | CS8602/CS8625 | GameProfilingSystem.cs | diagnostics display/runtime resource | null-forgiving for null depth target; local tag get cast |
| Entity collection state | CS8618/CS8603 | EntityComponentCollection.cs | default/constructor state | made owner entity nullable; `Get<T>` return type nullable |

## 5) Tests
- Re-ran `Stride.Engine.Tests`; no new tests added (changes were local nullability/guards and no behavior rewrite).

## 6) Fixes applied
- InputSystem event handlers now match nullable event sender contracts.
- DebugTextSystem now exits early when detached from Game and consistently uses the guarded game instance for context access.
- GameProfilingSystem uses explicit null target intent at render-target binding callsite.
- EntityComponentCollection now models detached/default construction by allowing nullable owner and nullable generic lookups.

## 7) Deferred issues
- STRIDE2000 buckets.
- UpdateEngine runtime invariants.
- processor matching/required-type policy.
- GPU/render lifecycle invariant buckets.
- Quarantine/Network async fire-and-forget warnings.
- Additional remaining buckets not safely local.

## 8) Warning results
- Focused warning count after: **398**
- Delta: **-16**
- Cleared/reduced buckets:
  - InputSystem CS8622 reduced (8 -> 0)
  - DebugTextSystem CS8602 reduced (6 -> 2 overall file-level bucket reduced)
  - EntityComponentCollection CS8618/CS8603 reduced locally
- Remaining top buckets still dominated by deferred policy/invariant areas.

## 9) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` — exit 0 — warnings only — pass — not truncated in log file.
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` — exit 0 — no warnings/errors in test run — pass — not truncated.

## 10) Next recommendation
Proceed with **finishing sweep 4** focused on remaining safe diagnostics/entity-local buckets before any policy-heavy UpdateEngine or processor-policy pass.
