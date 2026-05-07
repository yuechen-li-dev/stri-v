# 1230 — Focused warning checker exit-contract validation (M10j)

## 1) Files changed
- `striv/build/striv-check-focused-project.sh`
- `docs/stri-v/audits/1000+/1230-focused-warning-checker-exit-contract-validation.md`

## 2) Problem statement
The focused checker was already correctly detecting focused warnings in `Stride.Input` (`Focused warning count: 46`), but still returned process exit `0`. That breaks sustain gating because warning-bearing projects were indistinguishable from warning-clean projects by exit status.

## 3) Exit-code contract
Validated contract for `striv-check-focused-project.sh`:
- `dotnet build` failure → exit with the underlying build exit code.
- Build success + focused warnings (`count > 0`) → exit `4`.
- Build success + zero focused warnings (`count = 0`) → exit `0`.

## 4) Implementation change
Script logic was kept for focused warning detection and reporting, and the gate behavior was made explicit:
- Added explicit build-failure diagnostic line before returning the captured `dotnet build` exit code.
- Ensured focused-warning branch (`count > 0`) emits gate-failure message and exits `4`.
- Added explicit zero-warning success message and explicit `exit 0`.
- Kept existing focused warning extraction/filtering and top-code summary behavior unchanged.

## 5) Regression validation
| Project | Expected | Actual exit | Result |
|---|---:|---:|---|
| Stride.BepuPhysics | 0 | 0 | Pass |
| Stride.Core.Mathematics | 0 | 0 | Pass |
| Stride.Core.IO | 0 | 0 | Pass |
| Stride.FreeImage | 0 | 0 | Pass |
| Stride.Input | 4 | 4 | Pass |

## 6) Test/build validation
| Command | Exit code | First meaningful warning/error | Pass/fail | Output truncated |
|---|---:|---|---|---|
| `./striv/build/striv-check-focused-project.sh Stride.BepuPhysics` | 0 | none | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics` | 0 | none | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.IO` | 0 | none | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.FreeImage` | 0 | none | Pass | No |
| `set +e; ./striv/build/striv-check-focused-project.sh Stride.Input; input_status=$?; set -e; echo "Stride.Input status: $input_status"; test "$input_status" -eq 4` | 0 (`test` passed after expected nonzero from checker) | focused warnings present (`CS8618`, `CS8602`, `CS8625`) | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `CS8604` in `StriV.AssetPipeline/AssetPipeline.cs(72,26)` | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none emitted | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | `CS1030` (`PERF: Do not copy byte-for-byte.`) from `ObjectIdBuilder.cs` | Pass | Yes |

## 7) Deferred work
- CleanGraph focused-lane timeout repair (if still present under no-incremental focused script invocations).
- `Stride.Input` remaining Shine warning cleanup.

## 8) Recommended next task
Proceed with **M10k**: gesture/event pipeline tests plus targeted Shine for remaining `Stride.Input` warnings, unless a new blocker is observed during those validations.
