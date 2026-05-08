# Dominatus vendored source snapshot for Stri-V

This directory contains a vendored source snapshot of selected Dominatus projects for Stri-V integration and validation work.

Imported subset (M16a):
- `Dominatus.Core`
- `Dominatus.OptFlow`
- `Ariadne.OptFlow`
- `Dominatus.UtilityLite`

Not imported yet:
- server components
- Stride connector
- actuators
- LLM OptFlow
- console project
- tests/samples

Reason for vendoring:
- Current Dominatus packages are not yet available via NuGet in the way Stri-V needs for early strangler/bridge work.
- Source vendoring enables local bridge/proof development and build validation.

Rules for this vendored source:
- Do not edit vendored Dominatus code casually.
- If local patches are required to build or validate, document them in this README.
- Prefer upstreaming generally useful fixes back to Dominatus where possible.

Local patch log:
- None in M16a import; source copied as-is except for repository path placement and solution integration.
