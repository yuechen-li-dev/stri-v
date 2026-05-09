# 1990 — M20e Dominatus default lifecycle test path

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleTestDriver.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleOrchestratorCallsiteIntegrationTests.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/EntityLifecycleParityTests.cs`
- `docs/stri-v/audits/1000+/1990-m20e-dominatus-default-test-path.md`

Production files changed: **none**.

## 2) Task scope
- Dominatus orchestration via the engine-owned `RunEntityLifecycleFullCycleAsync(...)` seam is now the default test posture for this migrated lifecycle slice.
- Legacy direct lifecycle operations are retained only as an explicit parity/control baseline.
- No runtime rewiring or production-default behavior changes were made.

## 3) Default test path
- Added a small runtime test driver/fixture:
  - `EntityLifecycleFixture` centralizes scene/parent/child/entity-manager/processor setup.
  - `EntityLifecycleTestDriver.RunDominatusFullCycleAsync(...)` runs through:
    - `EntityManager.RunEntityLifecycleFullCycleAsync(...)`
    - `DominatusEntityLifecycleOrchestrator`
  - `EntityLifecycleTestDriver.CaptureSnapshot(...)` captures meaningful end-state evidence.
- Canonical default-path test now uses that Dominatus path directly:
  - `EntityLifecycleDefaultPath_FullCycle_UsesDominatusOrchestratorThroughEngineCallsite`.

## 4) Legacy control path
- Legacy direct sequence remains only in:
  - `EntityLifecycleTestDriver.RunLegacyDirectFullCycle(...)`.
- It is consumed by parity test only:
  - `EntityLifecycleParity_LegacyDirectAndDominatusOrchestratedFullCycle_ProduceSameSnapshot`.
- In-code comment marks it as control baseline only.

## 5) Tests and migration posture evidence
- `EntityLifecycleDefaultPath_FullCycle_UsesDominatusOrchestratorThroughEngineCallsite`
  - Path used: Dominatus orchestrated engine-owned callsite.
  - Asserts detached scenes/transform, processor detachment, manager processor set cleanup, add/remove counts, and child identity.
  - Proves Dominatus path is primary test path for this lifecycle slice.
- `EntityLifecycleParity_LegacyDirectAndDominatusOrchestratedFullCycle_ProduceSameSnapshot`
  - Path used: legacy direct control vs Dominatus orchestrated callsite.
  - Asserts snapshot equality across both flows.
  - Proves behavior parity while keeping legacy path as named control.

## 6) Dependency boundary verification
Command:
```bash
rg -n "StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true
```
Result: no matches.

## 7) Behavior compatibility
- Default engine runtime behavior unchanged.
- No direct Dominatus dependency added to `Stride.Engine`.
- Change is test posture only for the migrated lifecycle slice.

## 8) Validation results
1. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing repository build warnings (nullability/#warning) from dependencies during build; no new failures.
   - Result: pass
   - Output truncated: yes
2. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m20e-engine-focused.log`
   - Exit code: 0
   - First meaningful warning/error: existing `Stride.Engine` nullability warnings.
   - Result: pass
   - Output truncated: yes
3. Standard validation sequence from task (build, focused projects script, full requested test list, core build script)
   - Exit code: 0
   - First meaningful warning/error: existing repository warnings during build/test steps.
   - Result: pass
   - Output truncated: yes

## 9) Recommended next task
- **M20f migrate one real test fixture/callsite to use the engine-owned orchestrator seam**.
  - Rationale: continues migration in real usage while preserving current runtime defaults and boundary discipline.
