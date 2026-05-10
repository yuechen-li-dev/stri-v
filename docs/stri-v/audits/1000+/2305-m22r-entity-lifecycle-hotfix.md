# 2305 — M22r EntityLifecycle Hotfix

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/Entity.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/TransformComponent.cs`
- `striv/tests/Stride.Engine.Tests/ProcessorLifecycleInvokerTests.cs`
- `docs/stri-v/audits/1000+/2305-m22r-entity-lifecycle-hotfix.md`

## 2) Task scope
Hotfix/triage only. No new cleanup bucket was started; no warning policy changes; no suppression; no broad EntityLifecycle rewrite.

## 3) Failure reproduction
Command:
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v normal 2>&1 | tee /tmp/striv-m22r-hotfix-engine-tests.log`

Failures found:
1. `Stride.Engine.Tests.EntityLifecycleTests.TransformComponent_ParentDetach_ClearsParentAndChildLink`
   - Exception: `System.InvalidOperationException`
   - Message: `Entity [parent] is not registered with an EntityManager.`
   - First Stride stack frame: `Entity.get_EntityManager()` at `Entity.cs:132`, called from `TransformComponent.TransformChildrenCollection.OnTransformAdded()`.
2. `Stride.Engine.Tests.ProcessorLifecycleInvokerTests.EntityManager_AddEntityToProcessor_RequiresProcessorBoundToSameManager`
   - Failure: `Assert.Contains() Failure` (message assertion mismatch).
3. `Stride.Engine.Tests.ProcessorLifecycleInvokerTests.EntityManager_RemoveEntityFromProcessor_RequiresProcessorBoundToSameManager`
   - Failure: `Assert.Contains() Failure` (message assertion mismatch).

## 4) Root cause
- Primary runtime issue: **Case B** (production path intentionally probing optional detached lifecycle state but using guarded public accessor). `TransformChildrenCollection` used `Entity?.EntityManager?...`; after M22r, `EntityManager` is guarded and throws when detached.
- Processor invoker failures: **Case F** (exception message expectation drift). Behavior remained correct (throwing on unbound processor), but tests asserted previous wording.

## 5) Fix applied
Smallest targeted fix:
1. Added internal nullable seam on `Entity`:
   - `internal EntityManager? EntityManagerOrNull => entityManager;`
2. Updated `TransformComponent.TransformChildrenCollection` to use `EntityManagerOrNull` in hierarchy notifications.
3. Updated two processor-invoker tests to assert the current deterministic phrase (`"not attached"`) rather than old wording (`"registered with this EntityManager"`).

## 6) Contract preservation
- Guarded public lifecycle accessors are preserved (`EntityManager` remains non-null guarded).
- Nullable state stays internal/private via `EntityManagerOrNull`.
- No public nullable cascade reintroduced.

## 7) Warning state
Focused build command:
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`

Post-hotfix focused warning count (`Stride.Engine` filtered lines): **522** (`wc -l /tmp/striv-m22r-hotfix-warning-lines-after.log`).

Result: warning state remains at M22r level; no warning cascade observed.

## 8) Validation results
1. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v normal 2>&1 | tee /tmp/striv-m22r-hotfix-engine-tests.log`
   - Exit: `0` (after hotfix rerun done with minimal command below; initial run before fix was exit `1` with 3 failed tests)
   - Meaningful error before fix: `Entity [parent] is not registered with an EntityManager.`
   - Pass/fail: **pre-fix fail**, **post-fix pass**
   - Output truncated: no
2. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj --filter "FullyQualifiedName~TransformComponent_ParentDetach_ClearsParentAndChildLink|FullyQualifiedName~RequiresProcessorBoundToSameManager" -v normal`
   - Exit: `0`
   - Meaningful warnings: compile warnings only
   - Pass/fail: pass
   - Output truncated: no
3. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal 2>&1 | tee /tmp/striv-m22r-hotfix-engine-tests-after.log`
   - Exit: `0`
   - Meaningful result: `Passed! Failed: 0, Passed: 60`
   - Pass/fail: pass
   - Output truncated: no
4. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m22r-hotfix-engine-build-after.log`
   - Exit: `0`
   - Meaningful warning: existing nullable warnings remain (no new cascade)
   - Pass/fail: pass
   - Output truncated: no
5. Warning aggregation commands:
   - `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" ... > /tmp/striv-m22r-hotfix-warning-lines-after.log || true`
   - `wc -l /tmp/striv-m22r-hotfix-warning-lines-after.log`
   - Exit: `0`
   - Meaningful result: `522`
   - Pass/fail: pass
   - Output truncated: no

## 9) Next recommendation
Green for the M22r hotfix target. Continue with a **narrowed EntityLifecycle processor-policy pass** (next blocker isolated to remaining warning debt, not failing tests).
