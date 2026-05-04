# 930 - M6d Stride.BepuPhysics 5S Set in order validation

## 1. Files changed

- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/BepuSimulation.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Systems/PhysicsGameSystem.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Systems/CollidableProcessor.cs`
- `docs/stri-v/audits/930-m6d-bepuphysics-5s-set-in-order-validation.md`

## 2. 5S phase

M6c handled **Sort** (keep/quarantine/defer). M6d is focused on **Set in order** only: improving readability, navigability, and class/member intent without behavior changes.

Warning cleanup / **Shine** is intentionally deferred in this pass.

## 3. Target area

### Selected files

- `BepuSimulation.cs`
- `Systems/PhysicsGameSystem.cs`
- `Systems/CollidableProcessor.cs`

### Why this subset

This subset is the smallest coherent runtime integration slice that clarifies top-level update ownership and frame-to-simulation flow:

- simulation world owner (`BepuSimulation`),
- per-frame driver (`PhysicsGameSystem`),
- component attach/sync processor (`CollidableProcessor`).

### Explicitly left for later

- `BodyComponent.cs`
- `StaticComponent.cs`
- `CharacterComponent.cs`
- `Systems/ConstraintProcessor.cs`
- `Systems/ShapeCacheSystem.cs`
- `Definitions/**`
- `Constraints/**`
- quarantined gizmo files from M6c (`Systems/CollidableGizmo.cs`, `Systems/ConstraintGizmo.cs`)

## 4. Organization changes

### `BepuSimulation.cs`

- Added top-level class summary for runtime responsibility boundary.
- Added focused intent comments for:
  - simulation update component registry keying,
  - interpolation body tracking,
  - handle-indexed body/static lookup lists.
- No member signature changes, no field renames, no behavior changes.

### `Systems/PhysicsGameSystem.cs`

- Added class-level XML summary clarifying system role.
- Added comments documenting configuration ownership and soft-start reset timing intent.
- Added method XML summary for frame update behavior.
- No logic changes; flow and conditions unchanged.

### `Systems/CollidableProcessor.cs`

- Added class-level XML summary clarifying attach/detach and static transform synchronization duties.
- Added concise intent comments for static matrix cache purpose and service ownership boundaries.
- Added intent comment on static description in-place updates preserving existing handles/material allocations.
- No behavior changes.

## 5. Documentation changes

### `BepuSimulation.cs`

- XML comments added:
  - class responsibility summary.
- Invariant/intent comments added:
  - type-keyed update component registry rationale,
  - interpolation list role,
  - handle-to-list-index invariant for `Bodies`/`Statics`.
- Maintenance value: faster orientation around runtime ownership and critical indexing invariants.

### `Systems/PhysicsGameSystem.cs`

- XML comments added:
  - class summary,
  - `Update(GameTime)` summary.
- Invariant/intent comments added:
  - why soft-start reset is triggered in constructor,
  - relation to central `BepuConfiguration` simulation list.
- Maintenance value: clearer scene startup and frame execution intent.

### `Systems/CollidableProcessor.cs`

- XML comments added:
  - class summary.
- Invariant/intent comments added:
  - static cache write-avoidance purpose,
  - service-registry ownership boundary,
  - in-place static description update intent.
- Maintenance value: makes runtime synchronization path easier to follow and reason about safely.

## 6. Deferred cleanup

- Warning cleanup intentionally deferred (Shine not in scope), including existing `#warning` items in Bepu physics files.
- Broader member-order normalization deferred for remaining runtime components and processors.
- Future Set-in-order pass still needed for:
  - `BodyComponent.cs`, `StaticComponent.cs`, `CharacterComponent.cs`,
  - `Systems/ConstraintProcessor.cs`, `Systems/ShapeCacheSystem.cs`,
  - then `Definitions/**` and `Constraints/**`.

## 7. Validation results

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `find sources/engine/Stride.BepuPhysics/Stride.BepuPhysics -maxdepth 2 -type f | sort` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug` | 0 | `/workspace/stri-v/sources/core/Stride.Core/Storage/ObjectIdBuilder.cs(334,10): warning CS1030: #warning: 'PERF: Do not copy byte-for-byte.'` | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `/workspace/stri-v/striv/projects/StriV.AssetPipeline/AssetPipeline.cs(72,26): warning CS8604` | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | `/workspace/stri-v/sources/core/Stride.Core/Storage/ObjectIdBuilder.cs(334,10): warning CS1030` | Pass | Yes |

## 8. Next recommended step

**Continue Set-in-order for `Constraints/**`.**

Rationale: runtime flow clarity has now been improved in central integration files; next highest value is constraint component readability because those files are numerous and tightly coupled to runtime behavior, and can benefit from consistent member grouping/docs before Shine.
