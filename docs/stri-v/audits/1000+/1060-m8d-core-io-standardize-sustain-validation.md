# 1060 M8d Core IO Standardize/Sustain validation

## 1. Files changed
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/1000+/1060-m8d-core-io-standardize-sustain-validation.md`

## 2. Standardize/Sustain scope
M8a/M8b/M8c completed `Stride.Core.IO` Sort / Set-in-order / Shine. M8d scope is limited to Standardize/Sustain: lock the focused warning lane gate and update sustain documentation/reporting.

No `Stride.Core.IO` runtime behavior changes were made. VFS static initialization and related lifecycle refactor debt remain explicitly deferred.

## 3. Sustain test
- Test name: `FocusedWarningLane_CoreIO_HasZeroWarnings`
- Script invoked by the test: `striv/build/striv-check-focused-project.sh Stride.Core.IO`
- Strategy: reuses shared focused-lane helper in `CleanGraphSmokeTests` with async stdout/stderr capture, timeout-based wait, forced process kill on timeout, and fail-on-nonzero with full captured output.
- Expected condition: focused warning count for `Stride.Core.IO` is zero and script exits with code 0.

## 4. Documentation update
`docs/stri-v/building-core.md` focused warning lane section now includes:
- `Stride.Core.IO` in the focused check command list.
- `Stride.Core.IO` in completed zero-warning focused projects.

Existing `Stride.BepuPhysics` and `Stride.Core.Mathematics` documentation was preserved.

## 5. Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `./striv/build/striv-check-focused-project.sh Stride.Core.IO` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | `warning CS8618` in `Stride.FreeImage/Classes/FreeImageBitmap.cs` (inactive noise outside focused lane) | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `warning CS8604` in `StriV.AssetPipeline/AssetPipeline.cs` | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None reported | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | None reported | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | `warning CS1030` in `Stride.Core/Storage/ObjectIdBuilder.cs` during assembly processor build | Pass | Yes |

## 6. Current standard
Completed focused zero-warning projects now include:
- `Stride.BepuPhysics`
- `Stride.Core.Mathematics`
- `Stride.Core.IO`

## 7. Deferred work
- VFS static initialization refactor.
- `ApplicationObjectDatabase` explicit lifecycle.
- Possible `System.IO.Abstractions` migration.

## 8. Recommended next task
Recommend next project Sort as `Stride.FreeImage`, unless future validation exposes a blocker requiring reprioritization.
