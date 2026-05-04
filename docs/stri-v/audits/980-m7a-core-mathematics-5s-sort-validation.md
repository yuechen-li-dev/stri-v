# M7a - Stride.Core.Mathematics 5S Sort Validation

## 1. Files changed
- `sources/core/Stride.Core.Mathematics/BoundingSphere.cs`
- `sources/core/Stride.Core.Mathematics/Color3.cs`
- `sources/core/Stride.Core.Mathematics/Double2.cs`
- `sources/core/Stride.Core.Mathematics/Double3.cs`
- `sources/core/Stride.Core.Mathematics/Double4.cs`
- `sources/core/Stride.Core.Mathematics/Int2.cs`
- `sources/core/Stride.Core.Mathematics/Int3.cs`
- `sources/core/Stride.Core.Mathematics/Matrix.cs`
- `sources/core/Stride.Core.Mathematics/Plane.cs`
- `sources/core/Stride.Core.Mathematics/Quaternion.cs`
- `sources/core/Stride.Core.Mathematics/Ray.cs`
- `sources/core/Stride.Core.Mathematics/Vector2.cs`
- `sources/core/Stride.Core.Mathematics/Vector3.cs`
- `sources/core/Stride.Core.Mathematics/Vector4.cs`
- `striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`

## 2. 5S phase
This pass is **M7a Sort** only. Applied only safe mechanical removals of inactive compatibility blocks and documentation-only compile exclusion. No Set-in-order or Shine restructuring was performed.

## 3. Project inventory
- Compile inventory command output lines: **942** (`-getItem:Compile` XML output).
- Source file count under `sources/core/Stride.Core.Mathematics`: **52** files.
- Major type families observed: vectors (`Vector*`, `Double*`, `Int*`, `Half*`), matrices/quaternions (`Matrix`, `Quaternion`), geometry (`Bounding*`, `Plane`, `Ray`, `Rectangle*`, `Size*`), colors (`Color*`), and math helpers (`MathUtil`, spherical harmonics, angles).

## 4. Dead interop block audit
- Searched symbols: `SlimDX1xInterop`, `WPFInterop`, `XnaInterop`.
- `DefineConstants` for project: `TRACE;STRIDE_PLATFORM_LINUX;STRIDE_PLATFORM_DESKTOP;STRIDE_UI_SDL;STRIDE_GRAPHICS_API_VULKAN;STRIDE_ENGINE_WITHOUT_SHADER_COMPILER;STRIDE_ENGINE_WITHOUT_AUDIO;STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY;DEBUG`.
- None of the three interop symbols are defined in effective constants.
- Blocks found in: `BoundingSphere.cs`, `Color3.cs`, `Double2.cs`, `Double3.cs`, `Double4.cs`, `Int2.cs`, `Int3.cs`, `Matrix.cs`, `Plane.cs`, `Quaternion.cs`, `Ray.cs`, `Vector2.cs`, `Vector3.cs`, `Vector4.cs`.
- Change: removed full inactive `#if ... #endif` compatibility blocks for those three symbols.
- Behavior impact: none, because removed branches were not compiled for current clean graph constants.

## 5. NamespaceDoc / Module audit
- `NamespaceDoc.cs`: namespace documentation marker only (`internal class NamespaceDoc;`).
  - Action: excluded from this project build using `Compile Remove` in csproj.
  - Rationale: Sort-phase quarantine of documentation-only compile item, reversible and non-destructive.
- `Module.cs`: contains `[ModuleInitializer]` calling `AssemblyRegistry.Register(...)`.
  - Action: kept included.
  - Rationale: touches assembly registration/serialization ecosystem; not safe to remove in M7a without dedicated proof.

## 6. Serialization attribute classification
- Attributes present broadly across core math value types:
  - `[DataContract]`, `[DataMember]`, `[DataMemberIgnore]`, `[DataStyle(DataStyle.Compact)]`.
- Likely role: binary/yaml serialization contracts and stable field ordering/naming for assets/content.
- Risk if removed now: potential serializer contract drift, field ordering/name changes, AP/asset compatibility regressions.
- M7a action: classification only; no attribute stripping.
- M7b recommendation: proof-driven attribute review with serializer/AP regression matrix and contract snapshots.

## 7. System.Numerics classification
- Obvious future forwarding candidates (defer): `Vector2/3/4`, `Quaternion`, `Matrix` shape-aligned types.
- Unsafe/defer categories: types with Stride-specific serialization annotations, custom operators/implicit conversions, color/geometry structs, and non-`System.Numerics` semantic types (`Color*`, `Bounding*`, `Ray`, `Plane`, integer/double variants).
- M7a action: no forwarding implemented.

## 8. Classification table
| Source area/file group | Classification | Reason | Action |
|---|---|---|---|
| Core math structs and algorithms | Keep | Core domain model for Stri-V runtime math | No semantic/API changes |
| Interop `#if SlimDX1xInterop/WPFInterop/XnaInterop` blocks | Quarantine | Legacy compatibility paths inactive in clean graph | Removed inactive blocks |
| `NamespaceDoc.cs` | Quarantine | Documentation-only compile artifact | Excluded via csproj `Compile Remove` |
| `Module.cs` | Defer | Assembly registration may affect serialization/AP/runtime discovery | Keep for now; revisit with proof |
| Serialization attributes | Defer | Contract stability risk | Classified only |
| System.Numerics forwarding ideas | Defer | Needs compatibility/ABI/serializer proof | Classified only |

## 9. Warning delta
- Before warning baseline: attempted to build pre-change snapshot via worktree, but blocked by git-lfs smudge failure in environment.
- After focused project warning lines (`Stride.Core.Mathematics` grep pass): 4 lines, all `CS8618`.
- Focused project helper script reported 6 warnings (4 `CS8618`, 2 `MSB3026`).
- Project is **not** zero-warning.

## 10. Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet msbuild striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -getItem:Compile > /tmp/striv-m7a-math-compile.txt` | 0 | none | Pass | No |
| `wc -l /tmp/striv-m7a-math-compile.txt` | 0 | none | Pass | No |
| `find sources/core/Stride.Core.Mathematics -type f | sort > /tmp/striv-m7a-math-files.txt` | 0 | none | Pass | No |
| `rg -n "SlimDX1xInterop|WPFInterop|XnaInterop" ...` | 0 | none | Pass | No |
| `dotnet msbuild ... -getProperty:DefineConstants` | 0 | none | Pass | No |
| `rg -n "DataContract|DataMember|DataMemberIgnore|DataStyle|AssemblyRegistry|NamespaceDoc|Module" ...` | 0 | none | Pass | No |
| `git worktree add /tmp/striv-head HEAD && dotnet build ... (before baseline attempt)` | 128 | git-lfs smudge error (`missing protocol: ""`) | Fail (env blocker) | No |
| `dotnet build striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Mathematics` | 0 | `CS8618` in `SphericalHarmonics.cs` | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics` | 4 | focused warning summary (`CS8618`, `MSB3026`) | Fail (warning threshold) | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | external warning in AssetPipeline (`CS8604`) | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none significant | Pass | Yes (combined command stream) |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none significant | Pass | Yes (combined command stream) |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | repository-wide warnings, no test failure | Pass | Yes (combined command stream) |
| `./striv/build/striv-build-core.sh` | 0 | repository-wide warnings, no build failure | Pass | Yes (combined command stream) |

## 11. Project standard draft
- `Stride.Core.Mathematics` should contain portable, deterministic math primitives and geometry/color utilities required by core/runtime.
- Compatibility adapters for retired ecosystems (SlimDX/WPF/XNA interop blocks) belong to compatibility quarantine, not core active math path.
- Do not casually change: struct layouts, field names/order, equality/hash/operator semantics, serialization contracts.
- Any System.Numerics migration should be staged behind explicit compatibility proof (ABI, serializer, and perf checks).

## 12. Recommended next step
**M7b serialization attribute proof/removal** as the next step.
Reason: interop dead-path sort is complete; highest remaining cleanup value now sits in contract-attribute evidence gathering while preserving invariants.
