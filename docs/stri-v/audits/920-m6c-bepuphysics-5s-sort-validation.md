# 920 — M6c Stride.BepuPhysics virtual 5S Sort validation

## 1) Files changed
- `striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj`
- `docs/stri-v/audits/920-m6c-bepuphysics-5s-sort-validation.md`

## 2) 5S project scope
- **Target project:** `striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj`.
- **Why this project:** M6c requested a single-project virtual 5S Sort pass to improve convergence, and `Stride.BepuPhysics` is a bounded runtime integration unit with clear compile boundaries.
- **Phase boundary:** this work is **Sort** (classification + low-risk quarantine), not full cleanup, warning elimination, namespace surgery, or source deletion.

## 3) Compile inventory
- Compile inventory command (`dotnet msbuild ... -getItem:Compile`) reported **108** compile items.
- Source tree scan reported **108 files** total under the source root and **107 .cs** files under `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics` plus linked shared assembly info.
- Major compile groups (by top-level area under project source root):
  - `Definitions` (44)
  - `Constraints` (42)
  - `Systems` (7)
  - Root integration/component files (remaining single-file groups like `BepuSimulation.cs`, `BodyComponent.cs`, etc.)

## 4) Classification table

| Source area/file group | Classification | Reason | Action |
|---|---|---|---|
| `Definitions/**` (colliders, shapes, simulation defs, sim tests support types) | Keep / Defer | Core shape/collider/runtime contract code is central; some subareas (e.g. `SimTests` naming) are suspicious but still runtime-coupled in this project and not safely removable in Sort. | Keep included for now; revisit in later pass if decoupling evidence appears. |
| `Constraints/**` | Keep | Core runtime constraint descriptions/components for Bepu integration. | Keep included. |
| `Systems/PhysicsGameSystem.cs`, `Systems/CollidableProcessor.cs`, `Systems/ConstraintProcessor.cs`, `Systems/ShapeCacheSystem.cs`, `Systems/DefaultValueIsSceneBasedAttribute.cs` | Keep | Runtime system/process integration with engine scene/game systems and caches. | Keep included. |
| `Systems/CollidableGizmo.cs`, `Systems/ConstraintGizmo.cs` | Quarantine | Gizmo visualization/editor-facing behavior is non-core runtime and generated warnings; safe low-risk Sort quarantine target. | Excluded in clean project via `Compile Remove`. |
| Root runtime components (`BepuSimulation*`, `BodyComponent`, `StaticComponent`, `CharacterComponent`, `CollidableComponent`, masks/layers, module) | Keep / Defer | Central runtime API and simulation behavior; public/runtime semantics risk if changed. | Keep included. |

## 5) Exclusions applied

| Exclusion | Reason | Risk |
|---|---|---|
| `Systems/CollidableGizmo.cs` | Non-core gizmo/debug/editor-style visualization helper. | Low (runtime physics integration unaffected; compile exclusion only). |
| `Systems/ConstraintGizmo.cs` | Non-core gizmo/debug/editor-style visualization helper. | Low (runtime physics integration unaffected; compile exclusion only). |

## 6) Project-only warning delta
- **Before:** 5174 warning lines (cold/wider graph build from project entrypoint, many transitive project warnings).
- **After:** 24 warning lines (warm/incremental build; warnings from target project only in captured output).
- **Top warning codes before:** `CS8618 (1962)`, `CS8625 (938)`, `CS8600 (462)`, `CS8604 (426)`, `CS8603 (328)`.
- **Top warning codes after:** `CS1030 (22)`, `CS0169 (2)`.
- **Interpretation note:** the massive delta is mostly attributable to incremental/warm build behavior and transitive warning surfacing differences, plus removal of two `CS8767/CS8601/CS8625` gizmo-related warnings; not a full apples-to-apples cold rebuild warning burn-down.

## 7) Build/test validation

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet msbuild striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -getItem:Compile > /tmp/striv-m6c-bepu-compile.txt` | 0 | None | Pass | No |
| `find sources/engine/Stride.BepuPhysics/Stride.BepuPhysics -type f | sort > /tmp/striv-m6c-bepu-files.txt` | 0 | None | Pass | No |
| `rg -n "Debug|Editor|Gizmo|Navigation|Soft|_2D|Sample|Test|TODO|Obsolete|DataMember|Serialize|Render|Shape|Collider|Simulation|Processor|System|Component|Constraint|Bepu" ...` | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug 2>&1 | tee /tmp/striv-m6c-bepu-before.log` | 0 | `CS1030` in `Stride.Core/Storage/ObjectIdBuilder.cs` | Pass | Yes |
| `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug 2>&1 | tee /tmp/striv-m6c-bepu-after.log` | 0 | `CS1030` in `Definitions/Colliders/CompoundCollider.cs` | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `CS8604` warning in `StriV.AssetPipeline/AssetPipeline.cs` | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | Skipped test `StreamLiveness_DoesNotPruneWhenAccessUnknown` (not failure) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | `CS1030` in `Stride.Core/Storage/ObjectIdBuilder.cs` | Pass | Yes |

## 8) Project standard (Stride.BepuPhysics)
- **Belongs in core project:** runtime simulation integration, body/static/character/collidable components, constraint definitions, collider/shape definitions, runtime processors/systems essential to Stride.Engine gameplay execution.
- **Should be companion module later:** gizmo/editor/debug visualization helpers and any interactive tooling overlays.
- **Must not be reintroduced here without separate profile/project:** editor-only visualization/debug code paths, sample/demo/test-only helpers, and non-runtime companion features (navigation/soft-body/2D extensions) unless required by explicit runtime profile decisions.

## 9) Recommended next task
- **Next:** perform a **BepuPhysics Shine pass** focused on project-local warnings (`CS1030`, `CS0169`) and any remaining nullability warnings that appear under a controlled, comparable build mode for this project.
