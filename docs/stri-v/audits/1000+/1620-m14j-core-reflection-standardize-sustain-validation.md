# M14j — Stride.Core.Reflection Standardize/Sustain validation

## 1) Files changed
- docs/stri-v/building-core.md
- docs/stri-v/audits/1000+/1620-m14j-core-reflection-standardize-sustain-validation.md

## 2) Standardize/Sustain scope
M14a–M14i completed the Sort / boundary viability proof / Shine warning cleanup for `Stride.Core.Reflection` and reached focused warning count zero. M14j is a **gate lock** task only: it standardizes and sustains the focused warning gate by updating documentation and validating the focused gate scripts.

No `Stride.Core.Reflection` runtime behavior changes, no descriptor refactors, no warning policy changes, and no additional source cleanup were performed in this task.

## 3) Focused warning sustain
- Individual focused gate:
  - `./striv/build/striv-check-focused-project.sh Stride.Core.Reflection`
  - exit code: 0
  - focused warnings: 0
  - log: `/workspace/stri-v/striv/artifacts/logs/focused-build-Stride_Core_Reflection-20260508T203307Z.log`
- Completed focused batch gate:
  - `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
  - exit code: 0
  - all six projects pass with 0 focused warnings
- Summary artifact produced by batch checker:
  - `/workspace/stri-v/striv/artifacts/logs/focused-warning-summary.jsonl`

## 4) Documentation update
Updated `docs/stri-v/building-core.md` to lock `Stride.Core.Reflection` into the focused warning sustain lane by:
- adding explicit focused single-project gate command for `Stride.Core.Reflection`;
- extending the focused batch gate example list with `Stride.Core.Reflection`;
- extending the completed active focused zero-warning project list with `Stride.Core.Reflection`.

Preserved the focused-lane doctrine that these checks are build/script gates and **must run outside** `dotnet test` (tests must not spawn nested focused builds).

## 5) Current standard
Completed active focused zero-warning projects:
- `Stride.BepuPhysics`
- `Stride.Core.Mathematics`
- `Stride.Core.IO`
- `Stride.Input`
- `Stride.Games`
- `Stride.Core.Reflection`

Legacy/deferred exceptions:
- `Stride.FreeImage` remains a legacy native bridge exception.
- `Stride.Core.MicroThreading` remains a legacy compatibility subsystem pending replacement/migration.

## 6) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `./striv/build/striv-check-focused-project.sh Stride.Core.Reflection` | 0 | none | pass | no |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | none | pass | no |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | `CS0618` warnings from `TypeDescriptorFactoryCollectionFallbackTests` for intentional `OldCollectionDescriptor` coverage | pass | no |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | existing legacy warnings in transitive `Stride`/`Stride.Graphics` build (first seen `CS1030` in `Stride/Rendering/ParameterCollection.cs`) | pass | no |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | existing transitive warning set in non-focused projects (first seen `CS0436` in `Stride.Rendering/Properties/AssemblyInfo.cs`) | pass | no |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `CS8604` in `StriV.AssetPipeline/AssetPipeline.cs` | pass | no |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | pass | no |
| `./striv/build/striv-build-core.sh` | 0 | existing `RS1036` in `Stride.Core.Serialization.Generator/SerializationGenerator.cs` | pass | no |

## 7) Deferred work
- Deeper Reflection descriptor simplification after serialization/sourcegen milestones.
- Final non-generic collection support decision and eventual `OldCollectionDescriptor` deletion.
- Serialization/AP source generation maturation work.
- Next project 5S after this sustain lock.

## 8) Recommended next task
**Recommend next 5S target: `Stride.Core.Serialization`.**

Reason: it is upstream of serializer behavior and AssemblyProcessor/source-generation flows that directly constrain future `Stride.Core.Reflection` descriptor simplification and old collection fallback retirement. Progress there is likely to unlock higher-value cleanup with clearer convergence than broader engine/graphics surfaces at this point.
