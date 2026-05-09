# 2010 — M21a Stride.Engine Sort audit

## 1) Files changed

- `docs/stri-v/audits/1000+/2010-m21a-stride-engine-sort-audit.md` (this report).
- `docs/stri-v/stride-engine-sort-map.md` (sort map for future passes).

No `Stride.Engine` source behavior change was made in M21a.

## 2) Task scope

M21a executed a **Sort audit + first organization pass** only:

- re-baselined warnings for `Stride.Engine`;
- inventoried project structure;
- classified major subsystems by ownership/future direction;
- proposed doctrine/folder direction without broad source moves.

Explicitly out of scope for M21a:

- warning suppression;
- broad nullability cleanup (Shine);
- Dominatus migration expansion;
- runtime behavior changes.

## 3) Warning baseline

Command used:

```bash
dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Engine \
  --no-incremental
```

Focused warning extraction produced **964** project-path warning lines.

Total build warnings reported by dotnet build for project closure: **483**.

Top warning codes (focused extraction):

- CS8618: 340
- CS8625: 144
- CS8604: 84
- CS8602: 82
- CS8600: 70
- CS8603: 68
- CS8622: 52
- CS8601: 48
- CS8620: 22
- STRIDE2000: 14

Top file+code buckets:

- `Engine/ScriptComponent.cs` — CS8618 (28)
- `Rendering/Compositing/ForwardRenderer.cs` — CS8618 (24)
- `Engine/SceneInstance.cs` — CS8622 (22)
- `Engine/Design/CloneSerializer.cs` — CS8602 (20)
- `Engine/SceneSystem.cs` — CS8618 (18)
- `Engine/SceneInstance.cs` — CS8604 (18)
- `Engine/SceneInstance.cs` — CS8600 (18)
- `Engine/Game.cs` — CS8602 (16)
- `Rendering/ModelRenderProcessor.cs` — CS8604 (14)
- `Profiling/GameProfilingSystem.cs` — CS8602 (14)

Interpretation: warning mass is concentrated in lifecycle/side-effect orchestration surfaces and null-as-state seams.

## 4) Project inventory

Project root inventory highlights:

- major roots present: `Engine`, `Animations`, `Rendering`, `Updater`, `Profiling`, `Audio`, `Shaders.Compiler`, `Internals`;
- C# files counted: **226**;
- top root counts:
  - Engine: 113
  - Animations: 44
  - Rendering: 26
  - Updater: 24
  - Shaders.Compiler: 7
  - Profiling: 5
  - Audio: 4

`Stride.Engine.csproj` includes all `**/*.cs` then explicitly removes (Stri-V profile exclusions):

- `Rendering/Compositing/EditorTopLevelCompositor.cs`
- `Rendering/Compositing/ForwardRenderer.VRUtils.cs`
- `Shaders.Compiler/**/*.cs`
- `Audio/*.cs`
- `Engine/AudioEmitterComponent.cs`
- `Engine/AudioListenerComponent.cs`
- VR compositor files

This strongly indicates active runtime scope differs from legacy in-tree footprint.

## 5) Classification table

| Path/subsystem | Current role | Category | Reason | Recommended action |
| --- | --- | --- | --- | --- |
| `Engine/Entity.cs` | Core entity state root | Keep/model | Primarily state/data identity and component container semantics | Keep in model slice; nullability cleanup later |
| `Engine/EntityComponent.cs` | Base component model | Keep/model | Data/model surface with limited policy | Keep; clean contracts in Shine pass |
| `Engine/EntityComponentCollection.cs` | Component list + mutation hooks | Needs deeper audit | Mixed model collection + lifecycle coupling hooks | Separate pure collection API from orchestration callbacks |
| `Engine/TransformComponent.cs` | Transform data graph | Keep/model | Canonical scene graph state object | Keep; defer null-state cleanup |
| `Engine/Scene.cs` | Scene model/collection | Keep/model | Domain state representation | Keep as model anchor |
| `Engine/SceneInstance.cs` | Runtime scene activation/binding | Dominatus replacement candidate | Encodes lifecycle transitions and task-driven state | Extract policy transitions to Dominatus nodes |
| `Engine/EntityManager.cs` | Entity registration + processor coordination | Dominatus replacement candidate | Central lifecycle coordinator with ordering/dependency logic | Gradually split model registry vs lifecycle policy |
| `Engine/Processors/*` | Component-processing runtime | Needs deeper audit | Mixture of pure processing and lifecycle coupling | Triage processor-by-processor in M21b+ |
| `Engine/Processors/SceneSystem.cs` | Scene loading/root/compositor orchestration | Dominatus replacement candidate | Lifecycle policy hub with startup/load flow | Node/policy replacement target |
| `Engine/Processors/ScriptSystem.cs` | Script scheduling/startup flow | Dominatus replacement candidate | Scheduler/state transitions and script lifecycle ownership | Migrate policy to Dominatus lifecycle nodes |
| `Engine/Design/EntityCloner.cs` | Runtime clone execution | Actuator candidate | Side-effectful duplication pipeline rather than core model | Wrap via adapter/actuator boundary |
| `Animations/*` | Animation data + evaluators + runtime update | Needs deeper audit | Contains model curves plus evaluator/runtime orchestration mixed | Split model/evaluator/orchestrator responsibilities |
| `Audio/*` | Legacy audio processing/system | Obsolete/quarantine | Entire audio compile path currently removed in csproj | Keep quarantined pending explicit strategy; no deletion yet |
| `Rendering/*` | Render-facing runtime integration | Actuator candidate | Dominated by external/device/render-graph side effects | Move side-effect operations behind adapters over time |
| `Rendering/Compositing/*` | Compositor/runtime binding | Actuator candidate | Runtime binding/configuration operations; some excluded legacy files | Prioritize adapter boundaries before cleanup |
| `Rendering/Models*` + `ModelRenderProcessor` | Model render registration/update | Actuator candidate | Runtime renderer attachment/update side effects | Isolate registration/update actuators |
| `Rendering/Instancing*` + `Engine/Instancing*` | Instancing state + processing | Needs deeper audit | Mixed state containers and runtime render integration | Split state containers from runtime mutators |
| `Engine/Network/*` | Socket/router/platform network surfaces | Obsolete/quarantine | Legacy/mobile conditionals + NotImplemented paths | Quarantine decision pass before modernization/deletion |
| `Updater/*` | Reflection/IL update engine | Needs deeper audit | Contains STRIDE2000 suppressions and null-heavy pointer/object bridging | Strategic keep/replace decision required before cleanup |
| `Shaders.Compiler/*` | Remote/effect compiler plumbing | Obsolete/quarantine | Entire subtree excluded from compile by csproj | Mark as quarantined legacy tooling residue |
| platform-specific branches (Android/iOS/UWP mentions) | Compatibility branches in runtime code | Obsolete/quarantine | Signals historical compatibility surface not aligned to current profile | Keep in place until scoped quarantine pass |

## 6) Actuator candidates

High-confidence actuator candidates:

- `Engine/Design/EntityCloner.cs` and clone serializer execution path.
- `Rendering/Compositing/*` runtime binding flows (graphics compositor, scene camera renderer helpers).
- `Rendering/ModelRenderProcessor.cs`, `Rendering/Background/*`, `Rendering/Lights/*` runtime registration/mutation flows.
- `Profiling/*` runtime diagnostics emitters/systems.

These should keep public contracts but route side effects through explicit actuator/adapters in later milestones.

## 7) Dominatus replacement candidates

Primary lifecycle/policy hubs:

- `Engine/SceneSystem.cs`
- `Engine/Processors/ScriptSystem.cs`
- `Engine/SceneInstance.cs`
- policy-heavy portions of `Engine/EntityManager.cs`

These areas encode ordering/state transitions (including null-as-state seams) and align with eventual Dominatus node/policy replacement.

## 8) Obsolete/quarantine/delete candidates

Evidence-backed quarantine candidates:

- `Audio/*` and engine audio components (compile-removed in project file).
- `Shaders.Compiler/*` (compile-removed by project file).
- `Engine/Network/*` legacy/platform-branch-heavy area with `NotImplementedException` and mobile/UWP references.
- editor/legacy residues (e.g., `InternalsVisibleTo("Stride.Editor")`, editor-mode mentions).

Delete candidates were **not** actioned in M21a due to insufficient usage-proof and no-delete policy for this pass.

## 9) Recommended folder/doctrine organization

M21a recommendation: **docs-first, no broad code moves now**.

- Keep current physical layout during warning and lifecycle audits.
- Use `docs/stri-v/stride-engine-sort-map.md` as canonical ownership map.
- In M21b/M22 introduce staged conceptual slices first, then physical moves only when safe:
  - Model
  - Actuators
  - Lifecycle (Dominatus replacement)
  - Obsolete (quarantine)

## 10) Recommended M21b target

Recommended concrete next target: **`Engine/SceneInstance.cs` + `Engine/SceneSystem.cs` lifecycle cluster (test-first cleanup)**.

Why this target:

- high warning concentration (notably `SceneInstance` and `SceneSystem` buckets);
- directly tied to lifecycle policy boundaries identified for Dominatus replacement;
- enables targeted null-as-state cleanup with clear behavior seams.

Type of work:

- **test-first lifecycle cleanup** (not mechanical-only).

Must defer:

- broad rendering and updater cleanup,
- platform/network quarantine decisions,
- any large-scale folder moves.

## 11) Validation results

### Command 1

```bash
dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Engine \
  --no-incremental \
  2>&1 | tee /tmp/striv-m21a-engine-after.log
```

- Exit code: `0`
- First meaningful warning: `CS8767` in `Animations/AnimationChannel.cs`
- Pass/fail: **Pass** (build succeeds, warnings unchanged at 483)
- Output truncated: **No** (full log saved to `/tmp/striv-m21a-engine-after.log`)

### Command 2

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
- First meaningful warning/error: none (all selected projects pass, 0 warnings)
- Pass/fail: **Pass**
- Output truncated: **No** (summary + per-project logs emitted)

## Convergence state

**Success (Sort milestone)**: M21a completed the intended Sort capability (baseline + classification + direction map) without diverging into Shine cleanup or behavioral refactor.
