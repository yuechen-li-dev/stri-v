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

## M22 lifecycle-locality doctrine addendum:

Avoid fake modularity.

Do not split large files merely because they are large. A file is allowed to remain large if it owns one discrete behavior or lifecycle surface end-to-end and can be understood by reading it top-to-bottom.

Split files only when the split improves one of:

* lifecycle locality,
* responsibility clarity,
* testability,
* reduction of hidden coupling,
* subsystem independence.

For shared files/classes, do not assume sharing is good.

A shared abstraction must justify itself. Before placing any file/class into `Shared`, classify it:

1. True shared primitive

   * small,
   * mostly stateless,
   * obvious,
   * used by multiple subsystems without owning lifecycle policy.

2. Lifecycle policy / state machine

   * owns ordering,
   * owns state transitions,
   * owns scheduling,
   * owns hidden lifecycle rules.
   * Future direction: Dominatus node/policy, not generic shared helper.

3. Dispatch table candidate

   * current manager/factory is mostly choosing implementation by type/key/state.
   * Future direction: explicit dispatch table or registration surface.

4. Bad abstraction

   * shared only for terseness,
   * hides state,
   * causes cross-subsystem cascades.
   * Future direction: localize or duplicate inside subsystem if that makes each subsystem easier to reason about.

5. Needs audit

   * ownership unclear.
   * Do not move to `Shared` merely because ownership is unclear.

Important rule:
`Shared` must not become a junk drawer. Unknown ownership belongs in `NeedsAudit`, not `Shared`.

Codex may recommend splitting an originally shared manager/factory/helper into subsystem-local implementations when that reduces hidden state and makes behavior more explicit.

The bar:
If something is shaped like a real state machine or utility-decision surface, it should eventually be represented in Dominatus. Otherwise, it probably does not need to remain a shared implementation across the project.

Move doctrine:

* Preserve namespaces during physical moves.
* Do not combine file moves with behavior changes.
* Do not split files by length.
* Do not create tiny artificial modules.
* Prefer files that own one discrete lifecycle/functionality surface end-to-end.
