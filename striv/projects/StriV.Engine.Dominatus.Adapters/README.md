# StriV.Engine.Dominatus.Adapters

This project contains Stri-V-owned production adapters from legacy Stride APIs to Dominatus lifecycle actuator interfaces.

It does not rewire engine runtime behavior.

It exists outside `Stride.Engine` to avoid making the legacy engine core depend on Dominatus.

Null-as-detach calls are contained here only as compatibility wrappers until explicit engine detach APIs exist.

Future adapters may cover processor lifecycle, root scene lifecycle, script scheduling, and asset/runtime loading.
