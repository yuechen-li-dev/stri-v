# 2390 — M23c Dominatus-shaped EntityManager component-change contract slice

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityComponentChange.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityManager.cs`
- `striv/projects/Stride.Engine/Engine/EntityLifecycle/Entity.cs`
- `striv/tests/Stride.Engine.Tests/EntityManagerComponentChangeContractTests.cs`

## 2) Task scope
Implemented a Dominatus-shaped but Dominatus-free component change contract in `Stride.Engine` with explicit change kind and payload semantics. No Dominatus references were introduced and no processor matching algorithm rewrite was performed.

## 3) Dominatus shape review
Reviewed Dominatus architecture/authoring docs and source (`Dominatus.Core`, `Dominatus.OptFlow`) plus StriV bridge/runtime nodes and actuation handlers.
Observed shape: typed request/event payloads, explicit step-based actuation (`Ai.Act(...)`), and concrete event contracts (`*Requested`, `*Completed`) suitable for mailbox/actuator adaptation.

## 4) Before warnings
Focused warning count before: `318` (`/tmp/striv-m23c-engine-warning-lines-before.log`).
Entity lifecycle warning sites included null-flow around EntityManager component changes (`oldComponent/newComponent`) and dependent processor update parameters.

## 5) Contract design
Added:
- `EntityComponentChangeKind` with `Added`, `Removed`, `Replaced`.
- `EntityComponentChange` record struct carrying `Entity`, `OldComponent?`, `NewComponent?`, and `Kind`.
- Deterministic accessors (`AddedComponent`, `RemovedComponent`) and static factories (`Added`, `Removed`, `Replaced`).

Nullability now encodes payload semantics at one typed boundary instead of implicit old/new pair ambiguity.

## 6) Tests
Added payload-semantic tests:
- `EntityComponentChange_Added_HasExpectedPayload`
- `EntityComponentChange_Removed_HasExpectedPayload`
- `EntityComponentChange_Replaced_HasExpectedPayload`
- `EntityComponentChange_InvalidRequiredAccess_ThrowsDeterministicException`

## 7) Implementation slice
- Routed `Entity.OnComponentChanged(...)` through `EntityComponentChange` factories.
- Updated `EntityManager.NotifyComponentChanged(...)` and dependent-update helper to consume the typed change payload.
- Kept existing processor matching/removal/addition behavior and ordering intact.

Deferred: deeper processor policy routing and required-type dependency lifecycle redesign remain separate slices.

## 8) Dominatus dependency boundary
Boundary check command produced zero matches for forbidden Dominatus dependencies in `Stride.Engine`.

## 9) Warning results
Focused warning count after: `306` (`/tmp/striv-m23c-engine-warning-lines-after.log`).
Delta: `-12`.
Entity lifecycle component-change warning cluster narrowed (not fully eliminated).

## 10) Deferred issues
- Full processor policy routing harmonization.
- Required-type dependency map lifecycle cleanup.
- Associated-data lifecycle policy unification.
- Future adapter-stage Dominatus migration work (outside this slice).

## 11) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
  - exit: `0`
  - result: pass, warnings present
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - exit: `0`
  - result: pass (`78` tests)
- `rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj'`
  - exit: `0`
  - result: pass (0 matches)

## 12) Next recommendation
Continue with the next EntityManager component-change routing slice, focusing on remaining dependent-processor and required-type lifecycle contract edges.
