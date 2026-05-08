# StriV.Engine.Dominatus

`StriV.Engine.Dominatus` is the Stri-V-owned bridge surface between Stride engine lifecycle concepts and Dominatus runtime concepts.

## What this is

- A staging area for strangler-fig refactors of implicit Stride engine lifecycle/HFSM-like behavior into explicit Dominatus-oriented contracts.
- A place to define explicit blackboard keys, lifecycle events, actuator contracts, and node skeletons before behavior migration.

## What this is not

- This is **not** `Dominatus.StrideConn`.
- This is **not** a drop-in "Dominatus as an AI plugin for Stride" integration.
- This does **not** migrate runtime behavior yet.

## Current scope (M16b)

- Bridge surface only.
- No migration of `SceneSystem`, `EntityManager`, script scheduling, or processors.
- No runtime rewiring in `Stride.Engine` or `Stride.Games`.

## Future focus areas

- Scene lifecycle modeling.
- Entity attach/detach lifecycle modeling.
- Processor lifecycle modeling.
- Script scheduling lifecycle modeling.
- Asset/runtime loading lifecycle modeling.
- Graphics/device lifecycle modeling.

## Authoring rule

Bridge code should model explicit states, transitions, events, and actuator operations.
Do not use `null` as a state machine.
