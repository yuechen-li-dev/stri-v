# 2450 — M23i Stride.Engine rebaseline and next contract plan

## 1) Files changed
- `docs/stri-v/audits/1000+/2450-m23i-stride-engine-rebaseline-and-next-contract-plan.md`

## 2) Task scope
This pass is a post-M23h **rebaseline + hard-bucket classification** for `Stride.Engine` warnings, focused on identifying the largest remaining contract/policy clusters before selecting the next implementation slice.

- Rebaseline first (required).
- No warning suppression.
- No broad rewrite.
- No EntityManager/processor matching rewrite.
- No render pipeline rewrite.
- Dominatus-free scope preserved.

No source implementation slice was applied in this pass; this is an Option E (design/classification) outcome.

## 3) Current warning baseline

### Focused warning count
- Focused warning count: **278** lines in filtered warning log.
  - Source: `wc -l /tmp/striv-m23i-engine-warning-lines-before.log`.

### Warning code distribution
From `/tmp/striv-m23i-warning-codes-before.log`:

- CS8618: 62
- CS8602: 56
- CS8625: 54
- CS8601: 36
- CS8603: 34
- CS8600: 14
- CS8604: 12
- CS0675: 4
- CS8765: 2
- CS8620: 2
- CS0618: 2

### Top file buckets
Top buckets from `/tmp/striv-m23i-warning-buckets-before.log`:

1. `Engine/EntityLifecycle/EntityManager.cs CS8618` — 10
2. `Engine/RenderingLifecycle/ModelRenderProcessor.cs CS8601` — 8
3. `Engine/EntityLifecycle/Entity.cs CS8618` — 8
4. `Engine/EntityLifecycle/Entity.cs CS8603` — 8
5. `Engine/DiagnosticsProfilingLifecycle/GameProfilingSystem.cs CS8602` — 8
6. `Engine/RenderingLifecycle/LightShaftBoundingVolumeComponent.cs CS8625` — 6
7. `Engine/RenderingLifecycle/LightShaftBoundingVolumeComponent.cs CS8618` — 6
8. `Engine/RenderingLifecycle/Compositing/ForwardRenderer.cs CS8602` — 6
9. `Engine/EntityLifecycle/Processors/CameraProcessor.cs CS8625` — 6
10. `Engine/EntityLifecycle/EntityTransformExtensions.cs CS8603` — 6

### Folder/subsystem clusters
From `/tmp/striv-m23i-warning-folder-clusters-before.log`:

- `Engine/EntityLifecycle`: 30
- `Engine/RenderingLifecycle`: 28
- `Engine/AnimationLifecycle`: 9
- `Engine/ScriptLifecycle`: 6
- `Engine/SceneLifecycle`: 5
- `Engine/Shared`: 4
- `Engine/GameLifecycle`: 4
- `Engine/CloneLifecycle`: 4
- `Engine/NeedsAudit`: 1
- `Engine/DiagnosticsProfilingLifecycle`: 1

## 4) Classification table

| Rank | Bucket | Warning | Count | Subsystem | Category | Risk | Recommended action |
| ---: | ------ | ------- | ----: | --------- | -------- | ---- | ------------------ |
| 1 | `EntityLifecycle/EntityManager.cs` ctor events | CS8618 | 10 | EntityLifecycle / processor policy | event/delegate nullability; guarded lifecycle membership | Medium | Add explicit nullable event contract or initialization pattern validated by EntityManager tests; keep behavior unchanged. |
| 2 | `RenderingLifecycle/ModelRenderProcessor.cs` assignments | CS8601 | 8 | RenderingLifecycle | render/GPU lifecycle invariant | High | Defer as policy-heavy; address with targeted renderer lifecycle tests before annotation changes. |
| 3 | `EntityLifecycle/Entity.cs` ctor fields/serializers | CS8618 | 8 | EntityLifecycle | constructor/default state | Medium | Safe local sweep candidate: initialize/annotate constructor-only fields where runtime contract is already deterministic. |
| 4 | `EntityLifecycle/Entity.cs` returns | CS8603 | 8 | EntityLifecycle | guarded lifecycle membership | Medium | Tighten return contracts with explicit `?`/throw boundaries, backed by entity lifecycle tests. |
| 5 | `DiagnosticsProfilingLifecycle/GameProfilingSystem.cs` deref | CS8602 | 8 | DiagnosticsProfilingLifecycle | diagnostics display/runtime resource | Medium | Guard runtime resources and null-sensitive display data paths; local testable cleanup. |
| 6 | `RenderingLifecycle/LightShaftBoundingVolumeComponent.cs` events/null assigns | CS8625/CS8618 | 12 | RenderingLifecycle | event/delegate nullability; optional runtime resource | High | Group with rendering contract slice; avoid piecemeal fixes without render behavior checks. |
| 7 | `RenderingLifecycle/Compositing/ForwardRenderer.cs` | CS8602/CS8625/CS8604 | 12 | RenderingLifecycle | render/GPU lifecycle invariant | High | Defer to rendering slice with integration-level checks; do not local-fix blindly. |
| 8 | `EntityLifecycle/Processors/CameraProcessor.cs` null clears | CS8625 | 6 | EntityLifecycle / processor policy | processor matching policy | High | Keep current attach/detach policy; prepare tests around slot lifecycle before nullable changes. |
| 9 | `EntityLifecycle/EntityTransformExtensions.cs` | CS8603/CS8601 | 8 | EntityLifecycle | guarded lifecycle membership | Medium | Candidate for local contract tightening where parent/root-null is expected state. |
| 10 | `CloneLifecycle/*` serializer/cloner | CS8602/CS8625/CS8604/CS8618 | 10 | CloneLifecycle | clone serializer invariant | Medium | Keep as separate medium-risk slice after EntityLifecycle and rendering buckets. |

## 5) EntityLifecycle / processor policy analysis
EntityLifecycle remains the **largest cluster** (30 bucket entries) and still contains policy-heavy seams.

### Remaining warning kinds in EntityLifecycle
- Constructor/event defaults (`CS8618`) in `EntityManager`, `Entity`, `TransformComponent`, processor-local fields.
- Null-clearing/member lifecycle (`CS8625`) across processor attach/detach flows.
- Guarded lookups/returns (`CS8603`, `CS8600`, `CS8604`) in manager/collection and entity helper paths.
- Associated-data / processor interaction warnings in processors (e.g., `InstancingProcessor`, `ModelNodeLinkProcessor`, `LightShaftProcessor` nullability mismatch).

### Likely contract seams
- Required processor presence/ordering and late registration (`EntityManager` + `EntityProcessorCollection`).
- Processor membership transitions (`EntityComponentChange`, `EntityProcessorMembershipChange`) where old/new component null is semantically valid.
- Associated-data lifecycle (`EntityProcessor<T,TData>`) where validity depends on cross-processor state.
- Event lifecycle contract in `EntityManager` constructor and callback wiring.

### Proposed next EntityLifecycle slice (recommended)
**EntityManager constructor/event + processor collection contract slice** (test-first):
1. Add tests around event subscription lifecycle and processor retrieval defaults.
2. Normalize event nullability contract (nullable delegates or deterministic initialization pattern).
3. Clean local `EntityProcessorCollection.Get<T>()`/manager add-path null flow where contract is explicit.

This is the smallest high-value seam that reduces top warnings without rewriting matching policy.

## 6) Other top clusters
- **RenderingLifecycle** is a close second (28 bucket entries) and mostly high-risk render/GPU lifecycle invariants (`ForwardRenderer`, `ModelRenderProcessor`, light-shaft paths). Best handled as a dedicated slice, not mixed with EntityLifecycle.
- **UpdaterReflection** is no longer a top cluster in this baseline; no dominant residual UpdateEngine-adjacent bucket surfaced in top counts.
- **Game/Script/Scene/Shared** are smaller and mostly local nullability/constructor contracts; these are final-sweep candidates after top policy slices.

## 7) Implementation slice
No implementation was applied in M23i.

Rationale: the largest remaining warnings are policy/contract heavy (EntityLifecycle + RenderingLifecycle). A forced patch now would likely fake-fix or create behavior risk. Classification-first is the convergent outcome for selecting a safe, test-backed next slice.

## 8) Deferred issues
- Policy-heavy EntityManager / processor matching and membership contracts.
- Associated-data and required-type dependency lifecycle contracts across processors.
- Rendering/GPU lifecycle invariants (`ForwardRenderer`, `ModelRenderProcessor`, light-shaft flows).
- Low-priority residual local warnings (Script/Scene/Shared/Game) to handle in a final sweep.

## 9) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m23i-engine-before.log` | 0 | `CS8625` in `GraphicsCompositorHelper.cs(24,160)` | Pass | No (log captured to file; terminal preview truncated) |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m23i-engine-before.log | grep -E "striv/projects/Stride.Engine|/striv/projects/Stride.Engine|Stride.Engine.csproj" > /tmp/striv-m23i-engine-warning-lines-before.log || true` | 0 | n/a | Pass | No |
| `wc -l /tmp/striv-m23i-engine-warning-lines-before.log` | 0 | n/a | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m23i-engine-warning-lines-before.log | sort | uniq -c | sort -nr > /tmp/striv-m23i-warning-codes-before.log` | 0 | n/a | Pass | No |
| `sed -E 's#.*striv/projects/Stride.Engine/([^(:]+).*warning ((CS|CA|NU|STRIDE)[0-9]+).*#\1 \2#' /tmp/striv-m23i-engine-warning-lines-before.log | sort | uniq -c | sort -nr > /tmp/striv-m23i-warning-buckets-before.log` | 0 | n/a | Pass | No |
| `awk '{print $2}' /tmp/striv-m23i-warning-buckets-before.log | awk -F/ '{print $1 "/" $2}' | sort | uniq -c | sort -nr > /tmp/striv-m23i-warning-folder-clusters-before.log` | 0 | n/a | Pass | No |
| `grep -n "EntityLifecycle\|EntityManager\|EntityProcessor\|Processor\|RequiredTypes\|AssociatedData\|ComponentDatas\|CheckEntity\|ProcessEntity" /tmp/striv-m23i-engine-warning-lines-before.log || true` | 0 | n/a | Pass | No |
| `sed -n '1,1120p' striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityManager.cs` | 0 | n/a | Pass | No |
| `sed -n '1,980p' striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessor.cs` | 0 | n/a | Pass | No |
| `sed -n '1,680p' striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessorCollection.cs` | 0 | n/a | Pass | No |
| `sed -n '1,420p' striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityComponentChange.cs` | 0 | n/a | Pass | No |
| `sed -n '1,420p' striv/projects/Stride.Engine/Engine/EntityLifecycle/EntityProcessorMembershipChange.cs` | 0 | n/a | Pass | No |
| `rg -n "EntityManager|EntityProcessor|RequiredTypes|requiredTypes|RequiredType|Dependencies|ComponentDatas|AssociatedData|ProcessEntityComponent|CheckEntityComponent|CheckEntityComponentWithProcessors|EntityComponentChange|EntityProcessorMembershipChange|ApplyProcessorMembershipChange|entityMatch|entityAdded|oldComponent|newComponent|= null|== null|!= null|return null|!" striv/projects/Stride.Engine/Engine/EntityLifecycle striv/tests/Stride.Engine.Tests -g '*.cs' > /tmp/striv-m23i-entity-policy-search.txt` | 0 | n/a | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none (tests passed) | Pass | No |

## 10) Next recommendation
Choose **Option A** next: a test-first **EntityManager / processor policy slice** focused on constructor/event nullability contract and processor collection retrieval/add lifecycle, while explicitly avoiding processor matching rewrites.

Reasoning:
- EntityLifecycle remains the largest cluster.
- It contains a mix of medium-risk local contracts and high-risk policy seams.
- A narrow event/collection contract slice is the best convergence path before rendering lifecycle work.
