# 1020 M7e — Stride.Core.Mathematics serialization attribute proof validation

## 1) Files changed
- sources/core/Stride.Core.Mathematics/*.cs (21 files; DataStyle attributes removed only)
- docs/stri-v/audits/1000+/1020-m7e-core-mathematics-serialization-attribute-proof-validation.md

## 2) Problem statement
`Stride.Core.Mathematics` still carried historical serialization/editor annotations. The goal was to prove which annotations remain load-bearing in the clean Stri-V graph and remove only those proven non-value-producing.

## 3) Attribute inventory
Inventory command:
- `rg -n "\[DataContract|\[DataMember|\[DataMemberIgnore|\[DataStyle|AssemblyRegistry|ModuleInitializer" sources/core/Stride.Core.Mathematics striv/projects/Stride.Core.Mathematics`

Counts before change (global):
- DataContract: 35
- DataMember: 65
- DataMemberIgnore: 44
- DataStyle: 21

Representative families: vector/matrix primitives (`Vector*`, `Matrix`, `Quaternion`), color types (`Color*`), integer/double tuples (`Int*`, `Double*`), rectangles/sizes, and `AngleSingle`.

## 4) Dependency/proof audit
Reference audit command:
- `rg -n "DataContract|DataMember|DataMemberIgnore|DataStyle|AssemblyRegistry|Register\(|ModuleInitializer|DataSerializer|ContentSerializer" ...`

Findings:
- `Stride.Core.Reflection` consumes these attributes in `ObjectDescriptor` for member discovery/style/modes.
- `Stride.Core.AssemblyProcessor` discovers `[DataContract]` and member shape to generate serializers.
- `Module.cs` contains `[ModuleInitializer]` and `AssemblyRegistry.Register(..., AssemblyCommonCategories.Assets)`.

Critical proof trial:
- A full removal trial (DataContract/DataMember/DataMemberIgnore/DataStyle) was executed first and **failed** in `./striv/build/striv-build-core.sh` with AP serializer errors (example: `List<Color3>` in `LightProbeComponent.Coefficients` no longer had valid serializer metadata after contract removal).
- Therefore `DataContract`/`DataMember`/`DataMemberIgnore` are still load-bearing in clean graph runtime/build serialization.

## 5) Removal decision
| Candidate | Decision | Rationale |
|---|---|---|
| DataContract | keep | Removal breaks AP serializer generation during clean core build. |
| DataMember | keep | Required for generated serialized member shape and ordering. |
| DataMemberIgnore | keep | Removal risks AP including unsupported members; part of valid serializer shape constraints. |
| DataStyle | remove | Removal validated across focused build, clean graph tests, and core build without failures. |
| Module.cs | keep | Contains module initializer + assembly registration; not proven dead, and no failing proof justified safe removal. |

## 6) Changes applied
- Removed `[DataStyle(DataStyle.Compact)]` from 21 mathematics files.
- Kept `[DataContract]`, `[DataMember]`, `[DataMemberIgnore]`.
- Kept `Module.cs` unchanged.
- No field/property renames, order changes, layout changes, constructor changes, or algorithm changes.

## 7) Validation results
- `./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics` → exit 0, pass.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` → exit 0, pass (6/6).
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v normal` → exit 0, pass (build/test host success).
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v normal` → exit 0, pass (build/test host success).
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` → exit 0, pass (5/5).
- `./striv/build/striv-build-core.sh` → exit 0, pass.

Failure evidence from rejected trial (not final patch):
- full attribute removal caused `striv-build-core` failure in AP serialization stage (missing valid serializer for `Stride.Core.Mathematics.Color3` in engine graph).

## 8) Serializer/runtime risk assessment
What is proven:
- DataStyle is non-load-bearing for this clean graph lane.
- DataContract/DataMember/DataMemberIgnore remain load-bearing for AP/runtime serializer viability.

What is not proven:
- Legacy YAML/Quantum/editor compatibility details were not independently reintroduced/tested here.

Compatibility stance:
- This change intentionally drops DataStyle compact-style metadata from math contracts while preserving serializer viability and runtime behavior.

## 9) Focused warning sustain
- Focused lane remains zero-warning for `Stride.Core.Mathematics` after final patch.

## 10) Recommended next task
- `Stride.Core.Mathematics` additional serialization proof: targeted serializer smoke tests for `Vector3`, `Matrix`, `Quaternion`, `SphericalHarmonics` to explicitly prove style metadata irrelevance at runtime binary serialization layer.
