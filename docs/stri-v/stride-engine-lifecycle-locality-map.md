# Stride.Engine lifecycle-locality map (M22a)

This map defines `Stride.Engine` subsystems as **modules of lifecycle locality** for M22+ work.

## Doctrine

A subsystem groups files whose initialization, teardown, mutation, and warning cascades belong together.

## Proposed groups

- **EntityLifecycle**: entity/component ownership, transform graph semantics, entity manager/processor coordination.
- **SceneLifecycle**: scene membership, root scene activation, scene instance/system transitions.
- **ScriptLifecycle**: script components, async/sync/startup scripts, scheduler and script systems/processors.
- **CloneLifecycle**: clone graph composition and clone serializer/execution flow.
- **RenderingLifecycle**: model/render/instancing/compositor and renderer/processors.
- **GameLifecycle**: game bootstrap, services, update/draw lifecycle, game settings integration.
- **AnimationLifecycle**: animation update/evaluator runtime lifecycle.
- **DiagnosticsProfilingLifecycle**: runtime diagnostics/profiling systems.
- **UpdaterReflection**: updater/reflection/compiled update graph.
- **Shared**: cross-subsystem primitives/contracts only.
- **Quarantine**: excluded legacy/platform/editor/tooling residues.
- **NeedsAudit**: ownership unclear; do not force into Shared.

## First move recommendation (M22b)

**ScriptLifecycle** is the recommended first physical move target:
- coherent file set;
- recently cleaned (M21h) with known warning clusters;
- medium dependency surface but manageable;
- namespaces can remain unchanged;
- low-to-medium build risk vs rendering/scene topologies.
