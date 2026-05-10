# M23e — EntityManager processor-membership contract slice

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessorMembershipChange.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityManager.cs`
- `striv/tests/Stride.Engine.Tests/EntityManagerProcessorPolicyTests.cs`
- `docs/stri-v/audits/1000+/2410-m23e-entitymanager-processor-membership-contract-slice.md`

## 2) Task scope
This slice introduces a concrete typed processor-membership payload in `Stride.Engine` and routes one local membership decision seam through it (add/remove/revalidate intent), while preserving existing processor matching behavior and ordering semantics.

No generic event bus was added. No matching rewrite was performed.

## 3) Dominatus shape reference
Read:
- `striv/external/Dominatus/ARCHITECTURE.md`
- `striv/external/Dominatus/AUTHORING_GUIDE.md`

Influence used:
- explicit typed payloads;
- explicit operation kind enums;
- local routing through a narrow apply seam.

Not used:
- no Dominatus runtime APIs;
- no `Ai.Act`/`Ai.Await` references;
- no actuator/handler dependency.

## 4) Before warnings
Command: `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`

Focused warning count before: `302`.

EntityLifecycle-relevant lines included (examples):
- `EntityManager.cs` (`CS8618`, `CS8600`, `CS8604`, `CS8625`)
- `EntityProcessor.cs` (`CS8602`)
- related lifecycle files already in baseline

## 5) Contract design
Added internal payload:
- `EntityProcessorMembershipChangeKind`: `Added`, `Removed`, `Revalidated`
- `EntityProcessorMembershipChange`: `(EntityProcessor Processor, Entity Entity, EntityComponent Component, Kind)`
- Static factories: `Added(...)`, `Removed(...)`, `Revalidated(...)` with null guards.

Semantics:
- `Added`: initial/processor-discovery path membership evaluation.
- `Removed`: forced removal path.
- `Revalidated`: existing processor list re-check path when change affects current component type.

Why mailbox/actuator-ready:
- explicit intent object decouples policy decision (`Kind`) from actuation (`ProcessEntityComponent(...)`) via a single apply helper.

## 6) Tests
Added payload contract tests:
- `EntityProcessorMembershipChange_Added_HasExpectedPayload`
- `EntityProcessorMembershipChange_Removed_HasExpectedPayload`
- `EntityProcessorMembershipChange_Revalidated_HasExpectedPayload`

Added routing/ordering tests:
- `EntityManager_ComponentAdded_RoutesProcessorMembershipAddedOnce`
- `EntityManager_ComponentRemoved_RoutesProcessorMembershipRemovedOnce`
- `EntityManager_RequiredTypeProcessor_RevalidationPreservesExpectedOrder`

All existing required-type behavior tests remain and pass.

## 7) Implementation slice
Routed local processor membership operations through payload in:
- `CheckEntityWithNewProcessor(...)` (Added)
- `CheckEntityComponentWithProcessors(...)`:
  - existing map path: `Removed` or `Revalidated`
  - map initialization path: `Removed` or `Added`

Single seam:
- `ApplyProcessorMembershipChange(in EntityProcessorMembershipChange change)`

Remaining paths stay direct/deferred as-is; no behavior rewrite done.

## 8) Dominatus dependency boundary
Command:
- `rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`

Result: no matches.

## 9) Warning results
After build focused warning count: `302`.
Delta: `0`.

Bucket changes: none material; no warning cascade introduced.

## 10) Deferred policy issues
- Remaining required-type lifecycle complexity outside this slice.
- Associated data lifecycle remains in `EntityProcessor<TComponent, TData>` paths.
- Processor collection mutation/iteration ordering is unchanged and still delicate.
- Future Dominatus adapter/migration remains out of scope.

## 11) Validation results
1. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
   - exit: `0`
   - first meaningful warning: `CS8625` in `GraphicsCompositorHelper.cs`
   - pass/fail: pass
   - output truncated: yes (terminal capture)

2. `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
   - exit: `0`
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

3. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
   - exit: `0`
   - first meaningful warning: `CS8625` in `GraphicsCompositorHelper.cs`
   - pass/fail: pass
   - output truncated: yes (terminal capture)

4. `rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`
   - exit: `0`
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

## 12) Next recommendation
Next slice recommendation: **UpdateEngine contract design pass** (high warning concentration and architectural nullability cluster), while preserving EntityManager processor behavior already pinned by M23d/M23e tests.
