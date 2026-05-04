# 950 — M6f BepuPhysics final 5S Set-in-order validation

## 1) Files changed
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/_ThreeBodyConstraintComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Constraints/_FourBodyConstraintComponent.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Systems/ConstraintProcessor.cs`
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Systems/ShapeCacheSystem.cs`
- `docs/stri-v/audits/950-m6f-bepuphysics-final-5s-set-in-order-validation.md`

## 2) 5S phase
This pass is the **final Set-in-order pass before Shine** for `Stride.BepuPhysics`.

Scope was intentionally documentation/organization only:
- no behavior changes,
- no warning cleanup,
- no suppression,
- no serialization/AP shape changes.

Comments were expanded deliberately to act as a map for upcoming mechanical warning cleanup.

## 3) Target area
- Inventoried all files under `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/**`.
- Prioritized remaining high-value lifecycle/ownership files:
  - `_ThreeBodyConstraintComponent.cs`
  - `_FourBodyConstraintComponent.cs`
  - `Systems/ConstraintProcessor.cs`
  - `Systems/ShapeCacheSystem.cs`
- Left `BodyComponent.cs`, `StaticComponent.cs`, `CharacterComponent.cs`, `CollidableComponent.cs`, and remaining concrete constraints untouched in this pass to keep quality high and avoid comment confetti.

This closes the core mapping gap around multi-body constraint slots and processor/cache ownership boundaries that Shine needs.

## 4) Documentation/organization changes
### `_ThreeBodyConstraintComponent.cs`
- Added class-level XML docs clarifying authoring references vs runtime derived handles.
- Added per-slot XML docs for A/B/C mapping expectations.
- Behavior change status: **none**.

### `_FourBodyConstraintComponent.cs`
- Added class-level XML docs clarifying serialized body refs and derived runtime handle lifecycle.
- Added per-slot XML docs for A/B/C/D mapping expectations.
- Behavior change status: **none**.

### `Systems/ConstraintProcessor.cs`
- Added class-level XML docs documenting processor responsibility boundaries.
- Added lifecycle intent comments for service resolution, activate callback, and teardown callback.
- Behavior change status: **none**.

### `Systems/ShapeCacheSystem.cs`
- Added class-level XML docs for cache ownership and derived-data semantics.
- Added intent comments for shared buffer pool ownership, weak cache behavior, model-cache token contract, and dispose semantics.
- Added explicit XML docs on `Cache` record lifetime contract.
- Behavior change status: **none**.

## 5) Load-bearing invariants documented
- Constraint body slots are authoritative component/authoring state.
- Bepu constraint handles are derived runtime state and may be absent while unresolved/detached.
- `ConstraintProcessor` owns activation/deactivation wiring, not solver-handle mutation semantics.
- Constraint components own runtime handle attach/detach and reattachment idempotence.
- Shape cache inputs (`Model`, `DecomposedHulls`) are authoritative; extracted buffers/triangles are rebuildable runtime caches.
- `ShapeCacheSystem.Cache` object is an explicit lifetime token; releasing it permits GC-driven cache eviction.
- Shared `BufferPool` lifetime is owned by `ShapeCacheSystem` and cleared on service disposal.

## 6) Future Shine guidance
Safe/mechanical next categories in touched files:
- nullability/documentation alignment where lifecycle comments now identify delayed initialization (`_bepuConfiguration`, cache weak refs, runtime handles).
- warning triage around explicit `#warning` markers and non-null fields initialized in lifecycle hooks.

Still requires tests/manual validation:
- any attempted behavior-level optimization of constraint hot-update paths.
- any cache policy changes (weak/strong references, buffer ownership split).

Suggested first Shine targets:
1. `ConstraintProcessor.cs` (simple lifecycle nullability).
2. `_ThreeBodyConstraintComponent.cs`, `_FourBodyConstraintComponent.cs` (API docs already mapped).
3. `ShapeCacheSystem.cs` (careful with ownership/lifetime warnings).

## 7) Deferred cleanup
Intentionally deferred:
- All warning fixes and nullable annotation tightening.
- Potential API cleanup/refactors in body/static/character/collidable runtime components.
- Remaining concrete constraint component commentary expansion.

Potential follow-up Set-in-order (optional):
- representative concrete three/four-body constraint files if Shine uncovers mapping ambiguity.

## 8) Validation results
1. Command: `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug`
   - Exit code: `0`
   - Pass/fail: **PASS**
   - First meaningful warning/error: existing repo warning `ObjectIdBuilder.cs(334,10): warning CS1030`.
   - Output truncated: **yes** (tool token cap).

2. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: `0`
   - Pass/fail: **PASS**
   - First meaningful warning/error: existing warning `AssetPipeline.cs(72,26): warning CS8604` during dependency build.
   - Output truncated: **no**.

3. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - Pass/fail: **PASS**
   - First meaningful warning/error: none shown.
   - Output truncated: **no**.

4. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - Pass/fail: **PASS**
   - First meaningful warning/error: none shown.
   - Output truncated: **no**.

5. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - Pass/fail: **PASS**
   - First meaningful warning/error: none shown.
   - Output truncated: **no**.

6. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - Pass/fail: **PASS**
   - First meaningful warning/error: existing warning `ObjectIdBuilder.cs(334,10): warning CS1030`.
   - Output truncated: **yes** (tool token cap).

## 9) Next recommended step
Proceed to **BepuPhysics Shine pass** focused on project-local warnings, using the lifecycle/ownership notes added here as guardrails for mechanical nullability and warning cleanup.
