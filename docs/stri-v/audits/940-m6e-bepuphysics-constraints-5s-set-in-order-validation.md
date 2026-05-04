# 940 — M6e BepuPhysics Constraints 5S Set-in-order Validation

## 1) Files changed

- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/_ConstraintComponentBase.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/_ConstraintComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/_TwoBodyConstraintComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/_OneBodyConstraintComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/HingeConstraintComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/AngularMotorConstraintComponent.cs`
- `docs/stri-v/audits/940-m6e-bepuphysics-constraints-5s-set-in-order-validation.md`

## 2) 5S phase

This pass is **Set-in-order**, not Shine. The edits focus on making runtime ownership, constraint lifecycle, and component-to-Bepu mapping explicit for future mechanical refactors.

Warning cleanup, analyzer fixes, and broad style normalization were intentionally **not** targeted in this phase.

Comments/docs were made intentionally explicit (ChatGPT-like) where load-bearing intent, invariants, and sequencing are non-obvious.

## 3) Target area

### Files inventoried

Inventory command used:

```bash
find sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints -type f | sort
```

This enumerated all files under `Constraints/**` (base components, interfaces, and concrete constraints).

### Files touched

Selected a coherent, high-leverage subset:

- core lifecycle base types (`_ConstraintComponentBase`, `_ConstraintComponent`, `_OneBodyConstraintComponent`, `_TwoBodyConstraintComponent`)
- representative concrete constraints (`HingeConstraintComponent`, `AngularMotorConstraintComponent`)

### Files left for later

Most concrete constraints and interface docs remain for additional Set-in-order passes (for example: servo/limit variants and multi-body components).

### Why this subset

Base types are the architectural choke point for:

- body reference resolution timing,
- attachment/detachment ownership,
- runtime handle lifetime,
- and live description updates.

Documenting these first reduces risk for later warning cleanup across all concrete constraints.

## 4) Constraint architecture map

- Constraint components persist editor/serialized state (body references + per-constraint settings).
- `ConstraintProcessor` activates components once in-scene and provides `BepuConfiguration`.
- `ConstraintComponent<T>` then attempts attachment by validating:
  - component enabled state,
  - non-null body references,
  - available runtime body handles,
  - same target simulation for all bodies.
- On success, a Bepu solver constraint handle is created from the local `BepuConstraint` description.
- Property changes update `BepuConstraint`; `TryUpdateDescription()` applies to runtime only when currently attached.
- Reattachment is explicit and idempotent (`DetachConstraint()` first), keeping behavior stable across scene/load ordering.

For future nullability/refactor work, the key is that runtime handles are **derived state**, not primary state.

## 5) Organization/documentation changes

### `_ConstraintComponentBase.cs`
- Added class-level XML docs clarifying runtime materialization responsibilities.
- Added docs for `Enabled`, `Bodies`, `BodiesChanged`, `Activate`, and `TryReattachConstraint` to expose sequencing and safe-change boundaries.
- Behavior change: **none**.

### `_ConstraintComponent.cs`
- Added invariant comments around description ownership, reattach idempotency, and best-effort live updates.
- Clarified why `TryReattachConstraint()` rebuilds from current component state.
- Behavior change: **none**.

### `_TwoBodyConstraintComponent.cs`
- Added XML docs for class and `A`/`B` slots, including deferred runtime resolution note.
- Behavior change: **none**.

### `_OneBodyConstraintComponent.cs`
- Added XML docs for class and `A` slot.
- Behavior change: **none**.

### `HingeConstraintComponent.cs`
- Added class/constructor/property docs clarifying mapping to Bepu `Hinge` description and local-space axis semantics.
- Behavior change: **none**.

### `AngularMotorConstraintComponent.cs`
- Added class/constructor/property docs clarifying target velocity frame and default motor setup intent.
- Behavior change: **none**.

## 6) Load-bearing invariants documented

- Constraint runtime handles are created only after all body handles are resolved and simulation identity matches.
- Component state (`BepuConstraint` + body refs) is the authoritative source; runtime handle is rebuildable derived state.
- Reattach path detaches first for idempotency and to avoid stale solver entries.
- Live description updates are conditional/best-effort and intentionally no-op while detached.
- Processor/component split: processor activates/deactivates; component owns reattach validation and solver handle lifecycle.

## 7) Deferred cleanup

- Warning/analyzer cleanup remains deferred by design (Shine phase scope).
- Remaining `Constraints/**` concrete components still need equivalent intent/invariant documentation.
- Multi-body base components (`_ThreeBodyConstraintComponent`, `_FourBodyConstraintComponent`) were not touched in this pass.

## 8) Validation results

### Command 1
- Command: `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **pass**
- Output truncated: **yes** (terminal capture truncated for brevity)

### Command 2
- Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **pass**
- Output truncated: **yes**

### Command 3
- Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **pass**
- Output truncated: **yes**

### Command 4
- Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **pass**
- Output truncated: **yes**

### Command 5
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **pass**
- Output truncated: **yes**

### Command 6
- Command: `./striv/build/striv-build-core.sh`
- Exit code: `0`
- First meaningful warning/error: none
- Pass/fail: **pass**
- Output truncated: **yes**

## 9) Next recommended step

Continue **Set-in-order for remaining `Constraints/**`** so every concrete constraint gets the same level of lifecycle/ownership mapping before Shine warning removal begins.
