# Stride.Engine sort map (M21a)

## Purpose

This map captures the first ownership/direction sort for `striv/projects/Stride.Engine` after the Dominatus migration pause. It is intentionally **audit-first** and keeps a **no broad source move** policy for now.

## Classification legend

- **Keep/model**: core state/data surfaces that should remain in `Stride.Engine`.
- **Actuator candidate**: side-effect execution surfaces that should be wrapped behind actuator/adapters.
- **Dominatus replacement candidate**: lifecycle/policy orchestration that should become Dominatus nodes/policies.
- **Obsolete/quarantine**: legacy or compatibility-heavy areas not aligned to Stri-V runtime direction.
- **Delete candidate**: only when removal proof is strong (none actioned in M21a).
- **Needs deeper audit**: mixed ownership or unclear runtime relevance.

## Initial subsystem map

| Path/subsystem | Category | Direction |
| --- | --- | --- |
| `Engine/Entity.cs`, `EntityComponent*.cs`, `TransformComponent.cs`, `Scene.cs` | Keep/model | Keep as canonical model types; clean nullability in later Shine pass. |
| `Engine/EntityManager.cs`, `Engine/SceneInstance.cs` | Dominatus replacement candidate | Split policy/state transitions from model storage; keep model-facing APIs stable. |
| `Engine/Processors/*.cs` | Needs deeper audit | Keep pure data processors; separate lifecycle/order policy into Dominatus nodes. |
| `Engine/Processors/ScriptSystem.cs`, `Engine/SceneSystem.cs` | Dominatus replacement candidate | Candidate orchestration hubs for node-driven lifecycle replacement. |
| `Engine/Design/EntityCloner.cs` and clone serializers | Actuator candidate | Treat clone execution as adapter/actuator operation; keep contracts near model. |
| `Animations/*` | Needs deeper audit | Mixed model/evaluation/runtime orchestration; isolate evaluator side effects later. |
| `Rendering/Compositing/*` | Actuator candidate | Rendering graph binding/compositor attachment is side-effect dominated. |
| `Rendering/Models*`, `Rendering/Instancing*`, render processors | Actuator candidate | Runtime render registration/update should move behind adapters. |
| `Audio/*` | Obsolete/quarantine | Already excluded from project compile; quarantine strategy preferred before deletion. |
| `Shaders.Compiler/*` | Obsolete/quarantine | Excluded from compile in Stri-V core profile; treat as legacy tooling residue. |
| `Engine/Network/*` | Needs deeper audit | Legacy/mobile branch logic and incomplete paths suggest quarantine vs modernization decision needed. |
| `Profiling/*` | Actuator candidate | Runtime diagnostics side effects; keep optional and adapter-friendly. |
| `Updater/*` | Needs deeper audit | Reflection/IL-update machinery has suppression and nullability debt; evaluate strategic retention. |

## Folder/doctrine direction (no-move policy in M21a)

1. Do not physically move major folders yet.
2. Use docs-first mapping and subsystem tags in audits.
3. In M21b+, introduce explicit slices (conceptual first, then physical):
   - `Model/` (state/data)
   - `Actuators/` (runtime side effects)
   - `Lifecycle/` (Dominatus replacement targets)
   - `Obsolete/` (quarantined legacy)
4. Perform any physical moves only with project-file-safe staged migrations and focused validation.
