# Stride.Core.MicroThreading - Legacy Compatibility Subsystem

## Purpose

`Stride.Core.MicroThreading` is retained in Stri-V as a **temporary compatibility layer** for inherited Stride runtime paths that still rely on cooperative microthread scheduling.

## Why it is retained temporarily

M13a validated that this subsystem is not dead code and currently anchors active runtime paths including:

- `striv/projects/Stride.Engine/Engine/Processors/ScriptSystem.cs`
- `striv/projects/Stride.Engine/Engine/AsyncScript.cs`
- `striv/projects/Stride.BepuPhysics/BepuSimulation.cs`
- `striv/projects/Stride.Core.Serialization/Serialization/Contents/ContentManager.cs`

It currently provides capabilities these consumers depend on (cooperative scheduling, frame/tick await semantics, priority buckets, cancellation, microthread-local state, and synchronization-context identity).

## Strategic status in Stri-V

This subsystem is **not** the future architecture for Stri-V.

Long-term direction is:

- Dominatus as the scheduling/policy/lifecycle system.
- Channel/Task/ValueTask/CancellationToken-based work queues and dispatch pipelines.
- Explicit frame/tick dispatchers for frame-affine and physics tick-affine semantics.
- AsyncLocal/logical-context shims where context preservation remains necessary.

## Migration seam

M13b adds a small seam (`Compatibility/IFrameDispatcher.cs`) as a future-facing API shape. It is intentionally minimal and does not rewire existing behavior.

## Rules for current development

- Do **not** add new runtime consumers of MicroThreading without explicit approval.
- Do **not** treat MicroThreading internals as strategic future architecture.
- Prefer new Stri-V code to target future dispatcher/work-queue abstractions.
- Consumer migration is planned responsibility-by-responsibility and is intentionally deferred in this milestone.
