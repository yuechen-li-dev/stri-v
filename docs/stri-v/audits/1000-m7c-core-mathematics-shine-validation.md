# M7c — Stride.Core.Mathematics Shine Validation

## 1) Files changed
- `sources/core/Stride.Core.Mathematics/SphericalHarmonics.cs`
- `docs/stri-v/audits/1000-m7c-core-mathematics-shine-validation.md`

## 2) 5S phase
M7c is the **Shine** phase for `Stride.Core.Mathematics`: cleanup of focused warning debt without redesign.

- M7a (Sort) already removed inactive interop blocks and handled file classification.
- M7b (Set in order) already documented math/layout/serialization guardrails.
- M7c goal here is focused-lane warning cleanliness for `Stride.Core.Mathematics` while preserving behavior and serialized contract.

## 3) Before warnings
### Exact command
```bash
dotnet build striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Mathematics 2>&1 | tee /tmp/striv-m7c-math-before.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m7c-math-before.log | grep "Stride.Core.Mathematics" > /tmp/striv-m7c-math-warning-lines-before.log || true
wc -l /tmp/striv-m7c-math-warning-lines-before.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m7c-math-warning-lines-before.log | sort | uniq -c | sort -nr
```

### Warning count before
- Raw filtered warning lines: **4** (includes duplicate compiler summary entries).
- Unique warning sites in source: **2**.

### Warning codes before
- `CS8618` only.

### Exact warning sites before
- `sources/core/Stride.Core.Mathematics/SphericalHarmonics.cs(61,14)`: `Coefficients` non-nullable property not definitely assigned in serialization constructor path.
- `sources/core/Stride.Core.Mathematics/SphericalHarmonics.cs(171,14)`: `baseValues` non-nullable field not definitely assigned in serialization constructor path.

## 4) Fixes applied
### File: `sources/core/Stride.Core.Mathematics/SphericalHarmonics.cs`
- Warning addressed: `CS8618` at `Coefficients` property.
- Change made: Added initializer `= null!;` and lifecycle comment documenting runtime construction vs serializer materialization ownership.
- Behavior unchanged rationale: no runtime algorithm change; normal constructor still allocates `new TDataType[order * order]`; index formula and DataMember ordering unchanged.
- M7b guardrails preserved: serialized member identity (`Order`, `Coefficients`) and indexing remain untouched.

- Warning addressed: `CS8618` at `baseValues` field.
- Change made: Added initializer `= null!;` and comment documenting serializer constructor lifecycle.
- Behavior unchanged rationale: normal public constructor still allocates `new float[order * order]`; evaluate algorithm and coefficient math unchanged.
- M7b guardrails preserved: no coefficient indexing change, no algorithm change, no serialization attribute changes.

## 5) Serialization/lifecycle safety
- The internal parameterless constructors in both generic and concrete `SphericalHarmonics` types are retained for deserialization/materialization.
- `null!` is used as a nullable-analysis declaration, not behavioral initialization; it prevents false-positive CS8618 while preserving deferred member population patterns used by serializers.
- Public serialized shape is unchanged:
  - `Order` remains `[DataMember(0)]`.
  - `Coefficients` remains `[DataMember(1)]` with same name/type and accessors.
- No member ordering, field layout assumptions, or base coefficient/indexing logic was changed.

## 6) Tests
No new tests were added.

Reason: changes were nullable-flow annotations/comments only (`null!`) with no constructor behavior, algorithm, public API semantics, or serialized member shape changes.

## 7) After warnings
### Commands
```bash
dotnet build striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Mathematics 2>&1 | tee /tmp/striv-m7c-math-after.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m7c-math-after.log | grep "Stride.Core.Mathematics" > /tmp/striv-m7c-math-warning-lines-after.log || true
wc -l /tmp/striv-m7c-math-warning-lines-after.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m7c-math-warning-lines-after.log | sort | uniq -c | sort -nr
./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics
```

### Results
- Warning count after (focused): **0**.
- `striv-check-focused-project.sh Stride.Core.Mathematics`: **pass**.
- Project status in focused lane: **zero-warning clean**.

## 8) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Mathematics` (before) | 0 | `CS8618` at `SphericalHarmonics.cs` (`Coefficients`, `baseValues`) | Pass (with warnings) | No |
| `dotnet build striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Mathematics` (after) | 0 | None | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | External warning in another project: `StriV.AssetPipeline/AssetPipeline.cs(72,26) CS8604` | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None in test execution output | Pass | Yes (aggregated command output in tool view) |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | None in test execution output | Pass | Yes (aggregated command output in tool view) |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | Build emits pre-existing warnings in unrelated projects | Pass | Yes (large output) |
| `./striv/build/striv-build-core.sh` | 0 | Build emits pre-existing warnings in unrelated projects | Pass | Yes (large output) |

## 9) Standard/Sustain recommendation
Recommend proceeding into M7d Standardize/Sustain with a focused warning sustain gate for `Stride.Core.Mathematics` (e.g., CI step running focused build + `striv-check-focused-project.sh`).

## 10) Recommended next task
**Next task: M7d Standardize/Sustain for `Stride.Core.Mathematics`.**

Rationale: M7c objective is achieved (focused zero warnings) with no behavior/serialization/math changes, making this project ready to lock with sustain checks before moving to a new project.
