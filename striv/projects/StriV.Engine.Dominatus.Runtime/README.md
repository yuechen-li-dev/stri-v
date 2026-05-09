# StriV.Engine.Dominatus.Runtime

`StriV.Engine.Dominatus.Runtime` is an **opt-in** runtime host for Dominatus-backed Stri-V engine lifecycle scripts.

## Scope

- Lives outside `Stride.Engine`.
- Does **not** rewire or replace the default engine runtime loop.
- Uses lifecycle vocabulary from `StriV.Engine.Dominatus`.
- Uses production side-effect wrappers from `StriV.Engine.Dominatus.Adapters`.

## Current capability (M19a)

The first runner method executes the composed lifecycle add-flow for:

- scene attach,
- transform parent attach,
- processor system add,
- processor entity add.

## Future expansion

Future methods may cover cleanup/remove flows and root-scene lifecycle paths while remaining opt-in.
