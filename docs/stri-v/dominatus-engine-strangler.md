# Dominatus Engine Strangler Doctrine / Migration Map

## 1. Purpose

Stri-V is introducing Dominatus to formalize engine lifecycle behavior that already exists implicitly inside legacy `Stride.Engine` runtime paths.

Today, lifecycle and state-machine behavior is spread across managers, callbacks, service registries, null sentinels, scheduler nodes, and implicit ordering assumptions. In practice, this is an informal HFSM with hidden transition vocabulary.

The strangler direction is to replace hidden transitions with explicit lifecycle contracts:

- Dominatus events/messages,
- actuator interfaces,
- transition helpers,
- blackboard keys,
- node skeletons,
- production adapters outside `Stride.Engine`,
- tests and traces.

Null-as-state and null-as-transition patterns (for example, detach via `null`) are treated as symptoms of missing lifecycle vocabulary, not as the target design.

M16 proves a viable strangler path for lifecycle migration. It does **not** claim full migration completion.

---

## 2. Non-goals

This doctrine explicitly does **not** do the following:

- use `Dominatus.StrideConn`;
- add Dominatus as a plugin AI module to Stride runtime loops;
- rewrite `Stride.Engine` in one pass;
- add a direct Dominatus dependency to `Stride.Engine`;
- migrate `ScriptSystem` / scheduler behavior yet;
- change runtime behavior without test proof.

---

## 3. Project responsibilities

### `StriV.Engine.Dominatus`

Owns:

- event/message records,
- blackboard keys,
- actuator interfaces,
- transition helpers,
- node skeletons,
- lifecycle doctrine types.

Does **not** own:

- direct mutation of Stride runtime objects,
- production runtime adapter implementation,
- engine compatibility hacks.

### `StriV.Engine.Dominatus.Adapters`

Owns:

- production adapter classes wrapping legacy Stride APIs,
- containment of legacy null-as-detach calls,
- compatibility boundaries,
- opt-in actuator implementations.

Does **not** own:

- lifecycle vocabulary,
- Dominatus node semantics,
- direct runtime wiring into the engine update loop.

### `Stride.Engine`

Owns (for now):

- legacy entity/component/scene/processor objects,
- existing runtime behavior,
- compatibility surface,
- engine-owned narrow seams where needed.

Does **not** own:

- Dominatus dependency,
- future migrated lifecycle policy.

### Dependency direction

```text
Stride.Engine
  ↑ referenced by
StriV.Engine.Dominatus.Adapters
  ↑ references
StriV.Engine.Dominatus
  ↑ references
Dominatus.Core / Dominatus.OptFlow
```

Dominatus is not being attached to Stride as a behavior-AI plugin. Stride remains the legacy runtime/model surface while migration contracts live outside it.

---

## 4. Transition pattern

Canonical transition flow:

```text
Requested event
  -> transition helper
  -> actuator call
  -> completed event
```

Example:

```text
TransformParentAttachRequested
  -> TransformLifecycleTransition.AttachParentAsync(...)
  -> ITransformLifecycleActuator.AttachParentAsync(...)
  -> TransformParentAttached
```

Rules:

- transition helpers do not mutate Stride objects directly;
- actuators perform side effects;
- completed events are emitted only after actuator success;
- actuator exceptions propagate;
- no fake success event is emitted on failure;
- tests must prove both request/completed payload identity and side effect.

---

## 5. Legacy API containment

Legacy Stride detach APIs currently use `null`:

- `child.Transform.Parent = null`
- `entity.Scene = null`

Production adapters may temporarily contain these calls behind explicit methods and comments (for example `DetachFromParent`, `DetachFromScene`).

Doctrine rule: new bridge code must not use raw null-as-state directly; null detach is contained behind explicit adapter semantics.

Forward direction: future engine seams should replace null detach semantics with explicit lifecycle APIs.

---

## 6. Ordering doctrine

M16l established a concrete ordering policy under current Stride behavior:

- scene attach must happen before non-null transform parenting;
- a parented entity cannot be assigned to a non-null scene under current behavior.

Future lifecycle nodes and transition sequences must encode this ordering explicitly.

If an ordering constraint is discovered, it must be captured in one or more of:

- tests,
- doctrine,
- transition sequencing,
- future utility/decision rules.

Do not leave discovered ordering implicit.

---

## 7. Engine-owned seam doctrine

M16k proved the seam policy for protected/internal behavior access:

- do not use reflection or private/protected bypass from adapters;
- inspect ownership of the required behavior;
- add the smallest possible seam to the actual engine owner if public API is insufficient;
- seam APIs must be generic engine API, not Dominatus-specific.

First example:

- `EntityManager.AddEntityToProcessor(...)`
- `EntityManager.RemoveEntityFromProcessor(...)`

This seam preserves component-match-driven processor behavior while keeping `Stride.Engine` free of Dominatus references.

---

## 8. Test doctrine

Each strangler slice should include:

- behavior map test or equivalent inspection;
- request -> actuator -> completed-event tests;
- production adapter tests;
- failure propagation tests;
- no-runtime-rewiring assertions;
- integration composition test when multiple transitions compose.

Execution policy:

- run build/test commands sequentially when solution outputs can lock;
- do not run concurrent build/test commands due known MSBuild deps-file lock risk.

---

## 9. Migration stages

### Stage 0 — Map

Inspect existing Stride lifecycle behavior, ownership, and null/state patterns.

### Stage 1 — Vocabulary

Add lifecycle events/messages, actuator interfaces, and node skeletons.

### Stage 2 — Transition helper

Implement request -> actuator -> completed-event helper flow.

### Stage 3 — Test-local adapter

Prove behavior and contracts via test-local or fake adapters.

### Stage 4 — Production adapter outside `Stride.Engine`

Implement opt-in production adapter in `StriV.Engine.Dominatus.Adapters`.

### Stage 5 — Engine-owned seam (if required)

Add narrow owner seam in `Stride.Engine` only when existing API is insufficient.

### Stage 6 — Composition test

Prove transitions compose correctly through production adapters.

### Stage 7 — Runtime opt-in prototype

Only after doctrine clarity and composition proof.

---

## 10. Completed M16 proofs

Current proof coverage includes:

- transform parent attach/detach bridge transitions;
- scene membership attach/detach bridge transitions;
- processor system add/remove transitions;
- processor entity add/remove through the new `EntityManager` seam;
- composed scene + transform + processor lifecycle vertical-slice test through production adapters.

These prove migration viability without runtime rewiring and without direct Dominatus dependency in `Stride.Engine`.

---

## 11. Candidate next subsystems

Ranked candidates and migration posture:

1. **Root scene / `SceneInstance` lifecycle**
   - Value: direct next extension from scene-membership lifecycle and required for explicit active-root semantics.
   - Risk: medium (ordering/policy interactions).
   - Recommended stage: Stage 0 -> Stage 4 next.

2. **`EntityCloner` bounded operation lifecycle**
   - Value: compact, bounded operation proving operation-lifecycle strangling (vs long-lived object lifecycle only).
   - Risk: low-medium.
   - Recommended stage: Stage 1 -> Stage 4 pilot-friendly.

3. **Processor lifecycle expansion**
   - Value: deepens component-change and required-type transition coverage beyond M16 core proofs.
   - Risk: medium-high (breadth of processor behaviors).
   - Recommended stage: Stage 2 -> Stage 6 incremental.

4. **`ScriptSystem` / scheduler lifecycle**
   - Value: strategically important lifecycle center.
   - Risk: high (“Category 5” null/state-machine concentration and ordering complexity).
   - Recommended stage: deferred until more doctrine hardening and opt-in runtime path exists.

5. **Graphics device lifecycle**
   - Value: important platform lifecycle eventually.
   - Risk: very high blast radius.
   - Recommended stage: late, after several successful runtime opt-in migrations.

6. **Serialization/sourcegen integration path**
   - Value: related lifecycle path for persistence/integration correctness.
   - Risk: medium, cross-cutting.
   - Recommended stage: parallel design track after core runtime opt-in groundwork.

---

## 12. Do / Do Not checklist

### Do

- [ ] Name lifecycle states and transitions explicitly.
- [ ] Keep side effects inside actuator implementations.
- [ ] Keep `Stride.Engine` free of Dominatus references.
- [ ] Add tests before behavior changes.
- [ ] Use engine-owned seams instead of private/protected access bypass.
- [ ] Document and test ordering constraints.

### Do not

- [ ] Use null as new state-machine language.
- [ ] Add `?`/`!` nullability syntax everywhere as a pseudo-fix.
- [ ] Call protected/internal engine methods from adapters via reflection.
- [ ] Use `Dominatus.StrideConn`.
- [ ] Rewire runtime loops without opt-in tests.
- [ ] Let adapter projects define lifecycle vocabulary.
