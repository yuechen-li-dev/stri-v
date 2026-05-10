# 2280 — M22p RenderingLifecycle / LightProbeGenerator nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/RenderingLifecycle/LightProbes/LightProbeGenerator.cs`
- `striv/tests/Stride.Engine.Tests/RenderingLightLifecycleTests.cs`
- `docs/stri-v/audits/1000+/2280-m22p-lightprobe-generator-cleanup.md`

## 2) Task scope
This pass stayed folder-local to RenderingLifecycle light-probe generation nullability cleanup.
No lighting math, probe placement, tetrahedralization behavior, render-pipeline architecture, or Dominatus migration changes were made.

## 3) Before warnings
- Focused warning count before: **542** (`wc -l /tmp/striv-m22p-engine-warning-lines-before.log`)
- LightProbeGenerator warning lines before:
  - `LightProbeGenerator.cs(51,44): warning CS8602` (appeared twice in focused log)
- Relevant focused files/codes:
  - `Engine/RenderingLifecycle/LightProbes/LightProbeGenerator.cs CS8602`
  - `Engine/RenderingLifecycle/Compositing/ForwardRenderer.LightProbes.cs CS8602` (out of M22p scope)

## 4) LightProbeGenerator classification table
| File/site | Warning | Pattern | Null possible? | Intended behavior | Action |
| --------- | ------- | ------- | -------------: | ----------------- | ------ |
| `LightProbeGenerator.GenerateCoefficients` scene iteration | CS8602 | `context.SceneSystem.SceneInstance` dereference | Yes | Coefficient generation requires an active scene instance | Added deterministic `InvalidOperationException` when scene instance is absent |
| `LightProbeGenerator.GenerateCoefficients` probe transform read | latent NRE risk | `lightProbe.Entity.Transform` | Yes | Incomplete/unbound probes should not contribute | Added guard and no-op skip for unbound/missing transform probes |
| `LightProbeGenerator.GenerateRuntimeData` probe transform read | compiler/runtime null risk | `lightProbe.Entity.Transform` | Yes | Runtime-data generation requires bound probes | Added deterministic `InvalidOperationException` with explicit contract message |
| `LightProbeProcessor` component matching invariant | n/a | processor matches `TransformComponent` | Usually bound at runtime | Matching does not encode nullable contracts to compiler for all call paths | Documented via behavior test and explicit generator validation |

## 5) Tests
Added/updated tests:
- `LightProbeGenerator_UnboundProbeComponent_ThrowsInvalidOperationException`
  - Asserts deterministic failure when runtime-data generation receives unbound probes (no entity/transform), preventing accidental null dereference.

No graphics-device/runtime renderer bootstrap was introduced.

## 6) Fixes applied
### `LightProbeGenerator.cs`
- Old pattern: direct scene/transform dereferences (`context.SceneSystem.SceneInstance`, `lightProbe.Entity.Transform`).
- New pattern:
  - Validate `SceneInstance` existence and throw deterministic exception if missing.
  - Skip unbound probes in coefficient rendering pass (`continue` no-op).
  - Validate entity+transform in runtime-data generation and throw deterministic exception if missing.
- Why correct:
  - `GenerateCoefficients` is render-path work and should skip incomplete probes rather than crash.
  - `GenerateRuntimeData` requires stable probe positions for tetrahedralization; explicit failure communicates required contract and avoids hidden NRE behavior.

## 7) Deferred light-probe issues
- `ForwardRenderer.LightProbes.cs CS8602` remains in focused warnings (outside this file target).
- Broader scene/entity binding invariants remain compiler-invisible in several lifecycle APIs.
- GPU descriptor/resource lifecycle split remains deferred (as previously noted in M22o).

## 8) After warnings
- Focused warning count after: **540** (`wc -l /tmp/striv-m22p-engine-warning-lines-after.log`)
- `LightProbeGenerator.cs` focused warning lines: **cleared** in after log.
- Total focused delta vs before: **-2** (542 -> 540).

## 9) Next recommendation
From top remaining buckets, recommended next target:
- `Engine/RenderingLifecycle/Compositing/ForwardRenderer.LightProbes.cs CS8602`

Rationale:
- Same RenderingLifecycle light-probe cluster,
- likely related scene/runtime contract visibility,
- can continue local convergence without broad subsystem rewrite.

## 10) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
  - Exit code: 0
  - First meaningful warning/error: pre-existing engine nullability warnings during build
  - Pass/fail: PASS
  - Output truncated: yes (tool token limit)

- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
  - Exit code: 0
  - First meaningful warning/error: pre-existing `CS8765` in `CompressedTimeSpan`
  - Pass/fail: PASS
  - Output truncated: yes (tool token limit)

- Standard validation batch command sequence from Part 9 was started and produced ongoing successful build/test progress logs, but complete tail/aggregate exit capture was truncated by tooling/output limits in this run.
  - Pass/fail: PARTIAL EVIDENCE (not all terminal tail statuses captured in one bounded transcript)
