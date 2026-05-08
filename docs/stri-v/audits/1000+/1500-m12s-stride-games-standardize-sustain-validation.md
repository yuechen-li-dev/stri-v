# 1500 — M12s Stride.Games Standardize/Sustain validation

## 1) Files changed
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/1000+/1500-m12s-stride-games-standardize-sustain-validation.md`

## 2) Standardize/Sustain scope
M12a through M12r already completed Sort / Set in order / Shine for `Stride.Games`.

M12s is sustain-only: it locks `Stride.Games` into the completed active focused zero-warning gate documentation and validates that the focused single-project and focused batch gates pass.

No runtime behavior changes, no source cleanup/refactor in `Stride.Games`, no warning-policy changes, and no suppression additions were performed in this task.

## 3) Focused warning sustain
- Individual focused check (`Stride.Games`): **pass** (`exit 0`, focused warnings `0`).
- Completed focused batch gate (`Stride.BepuPhysics`, `Stride.Core.Mathematics`, `Stride.Core.IO`, `Stride.Input`, `Stride.Games`): **pass** (all `exit 0`, focused warnings `0`).
- Summary artifact produced by batch script:
  - `/workspace/stri-v/striv/artifacts/logs/focused-warning-summary.jsonl`

## 4) Documentation update
Updated `docs/stri-v/building-core.md` to:
- add the individual sustain command for `Stride.Games`:
  - `./striv/build/striv-check-focused-project.sh Stride.Games`
- update the completed focused batch example to include `Stride.Games`
- add `Stride.Games` to the completed active focused zero-warning project list
- keep `Stride.FreeImage` separated as the documented legacy bridge exception
- preserve the explicit note that focused warning checks are build/script gates and must not run under `dotnet test`

## 5) Current standard
Completed active focused zero-warning projects:
- `Stride.BepuPhysics`
- `Stride.Core.Mathematics`
- `Stride.Core.IO`
- `Stride.Input`
- `Stride.Games`

Legacy bridge exception:
- `Stride.FreeImage`

## 6) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `./striv/build/striv-check-focused-project.sh Stride.Games` | 0 | none | pass | no |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games` | 0 | none | pass | no |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | `MSB3026` transient copy retry warning while projects overlapped with concurrent run | pass | no |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `CS8604` in `StriV.AssetPipeline/AssetPipeline.cs(72,26)` | pass | no |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | pass | no |
| `./striv/build/striv-build-core.sh` | 0 | `CS1030` from `ObjectIdBuilder.cs` during AssemblyProcessor bootstrap build | pass | no |

## 7) Deferred work
- Future `Stride.Games` project splits:
  - windowing
  - desktop backends
  - graphics bridge/presentation
  - mobile quarantine deletion
- Future Dominatus lifecycle integration
- Real desktop runtime smoke validation (if/when desired)

## 8) Recommended next task
Recommended next 5S target: `Stride.Core.MicroThreading`.

Reason: it is a contained core subsystem with clear ownership boundaries and broad transitive utility, making it a high-leverage candidate for the same focused warning-lane 5S progression without coupling immediately into heavier rendering/runtime surface areas.
