# 1780 — M16m Dominatus Engine Strangler Doctrine Validation

## 1. Files changed

- `docs/stri-v/dominatus-engine-strangler.md`
- `docs/stri-v/audits/1000+/1780-m16m-dominatus-engine-strangler-doctrine-validation.md`

## 2. Task scope

M16m is documentation/doctrine only.

This task intentionally introduces no runtime behavior changes, no runtime rewiring, and no `Stride.Engine` functional modifications. The output is a durable migration doctrine and a validation report for future strangler tasks.

## 3. Doctrine summary

The doctrine defines Stri-V lifecycle migration as a strangler process that converts implicit engine lifecycle/HFSM behavior (managers, callbacks, service registries, null sentinels, scheduler nodes, implicit ordering) into explicit Dominatus contracts:

- typed lifecycle events/messages,
- actuator interfaces for side effects,
- transition helpers (`requested -> actuator -> completed`),
- blackboard/state semantics,
- node skeletons,
- production adapters outside `Stride.Engine`,
- tests and traces.

M16 is positioned as proof of path viability, not full migration completion.

## 4. Architecture boundary

Boundary and dependency direction are explicitly documented:

```text
Stride.Engine
  ↑ referenced by
StriV.Engine.Dominatus.Adapters
  ↑ references
StriV.Engine.Dominatus
  ↑ references
Dominatus.Core / Dominatus.OptFlow
```

Responsibility split:

- `StriV.Engine.Dominatus`: vocabulary/contracts/helpers.
- `StriV.Engine.Dominatus.Adapters`: production wrappers and compatibility containment.
- `Stride.Engine`: legacy runtime/model ownership plus narrow generic seams only.

No direct Dominatus dependency is introduced in `Stride.Engine`.

## 5. Transition pattern

Canonical lifecycle bridge pattern is standardized as:

```text
Requested event
  -> transition helper
  -> actuator call
  -> completed event
```

Rules captured:

- helpers do not mutate Stride directly,
- actuators own side effects,
- completion event only on success,
- exceptions propagate,
- no fake success-on-failure,
- tests must prove payload identity + side effect.

## 6. Lessons from M16 proofs

Key extracted lessons:

1. **Ordering constraints are real behavior contracts**
   - Scene attach must precede non-null transform parenting under current behavior.

2. **Legacy null semantics must be contained, not propagated**
   - Null detach remains allowed only behind explicit adapter methods.

3. **Engine-owned seams beat private/protected bypass**
   - M16k seam (`EntityManager.AddEntityToProcessor` / `RemoveEntityFromProcessor`) is the model.

4. **Adapter isolation is effective**
   - Production adapters can bridge legacy behavior while preserving `Stride.Engine` dependency purity.

5. **Composed lifecycle slices are viable**
   - M16l proves multi-transition composition through production adapters without runtime rewiring.

## 7. Recommended next task

**Recommendation: M16n root-scene lifecycle bridge proof.**

Justification:

- It is the nearest dependency-neighbor of proven scene membership lifecycle work.
- It extends existing ordering doctrine in a bounded, testable direction.
- It strengthens lifecycle policy around active root semantics before larger runtime opt-in work.
- It avoids premature jump to scheduler/ScriptSystem high-blast migration.

## 8. Validation

### Required commands

1) Command:

```bash
./striv/build/striv-check-focused-projects.sh \
  Stride.BepuPhysics \
  Stride.Core.Mathematics \
  Stride.Core.IO \
  Stride.Input \
  Stride.Games \
  Stride.Core.Reflection
```

- Exit code: `0`
- First meaningful warning/error: none observed
- Pass/fail: pass
- Output truncated: no

2) Command:

```bash
dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal
```

- Exit code: `0`
- First meaningful warning/error: `/workspace/stri-v/striv/projects/Stride/Rendering/ParameterCollection.cs(675,34): warning CS1030: #warning ...`
- Pass/fail: pass
- Output truncated: yes (tool output limit)

### Optional preferred commands

3) Command:

```bash
dotnet build striv/StriV.Core.slnx -c Debug -v minimal
```

- Exit code: `0`
- First meaningful warning/error: `/workspace/stri-v/sources/core/Stride.Core/Storage/ObjectIdBuilder.cs(334,10): warning CS1030: #warning ...`
- Pass/fail: pass
- Output truncated: yes (tool output limit)

4) Command:

```bash
./striv/build/striv-build-core.sh
```

- Exit code: `0`
- First meaningful warning/error: none observed
- Pass/fail: pass
- Output truncated: yes (tool output limit)

## Convergence state

**Success**: M16m doctrine and migration map delivered with validation evidence, and no runtime behavior changes introduced.
