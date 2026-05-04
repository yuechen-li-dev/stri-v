# M7b — Stride.Core.Mathematics 5S Set in order validation

## 1. Files changed
- `sources/core/Stride.Core.Mathematics/Matrix.cs`
- `sources/core/Stride.Core.Mathematics/Vector3.cs`
- `sources/core/Stride.Core.Mathematics/Quaternion.cs`
- `sources/core/Stride.Core.Mathematics/SphericalHarmonics.cs`
- `docs/stri-v/audits/990-m7b-core-mathematics-5s-set-in-order-validation.md`

## 2. 5S phase
- M7a performed **Sort** for `Stride.Core.Mathematics` (inactive interop blocks removed and deferred tracks identified).
- M7b performs **Set in order** by documenting refactor-critical invariants needed for safe future mechanical cleanup.
- **Shine/warning cleanup is intentionally deferred** in this pass (including existing CS8618 warnings).

## 3. Target area
### Files inspected
- `Matrix.cs`
- `Vector3.cs`
- `Quaternion.cs`
- `SphericalHarmonics.cs`
- plus keyword scan over `sources/core/Stride.Core.Mathematics/**`.

### Files touched
- `Matrix.cs`
- `Vector3.cs`
- `Quaternion.cs`
- `SphericalHarmonics.cs`

### Files intentionally left alone
- Other high-value files were not edited in this pass because existing docs already convey core semantics sufficiently for Set-in-order scope, and this pass prioritizes high-signal invariant notes over broad comment churn.

## 4. Refactor invariant documentation added
### `Matrix.cs`
- Added a struct-level refactor invariant remark clarifying that `StructLayout`, field set/order, and serialized shape are compatibility-sensitive.
- Added a guardrail note on `LayoutIsRowMajor` to preserve established matrix convention when touching multiplication/transform logic or any future System.Numerics forwarding.
- Protects future refactors from accidental layout/semantics drift.

### `Vector3.cs`
- Added a struct-level remark documenting that sequential layout and X/Y/Z order are load-bearing for binary shape and `Unsafe.BitCast` with `System.Numerics.Vector3`.
- Protects future cleanup/migration from swapping to non-bitwise conversion or reordering fields without proof.

### `Quaternion.cs`
- Added a struct-level remark documenting serializer shape + interop + `Unsafe.BitCast<System.Numerics.Quaternion>` coupling.
- Added an `IsNormalized` remark clarifying that normalization-sensitive paths and tolerance behavior (`MathUtil.ZeroTolerance`) must be preserved during forwarding/refactoring.
- Protects future BCL-forwarding and algorithm cleanup from semantic drift in edge cases.

### `SphericalHarmonics.cs`
- Added class-level invariant remarks documenting that `Order` + `Coefficients` member shape/order and coefficient indexing formula are serialized-contract sensitive.
- Added a constructor lifecycle remark explaining serialization-time deferred initialization for `Coefficients`.
- Protects future CS8618 fixes from breaking deserialization lifecycle behavior.

## 5. Serialization/layout/System.Numerics guardrails
- **Layout-sensitive types covered here**: `Matrix`, `Vector3`, `Quaternion`.
- **Serialization-sensitive types covered here**: `Matrix`, `Vector3`, `Quaternion`, `SphericalHarmonics<TDataType>`.
- **System.Numerics migration constraints (documented)**:
  - `Vector3` and `Quaternion` use `Unsafe.BitCast`; forwarding/replacement must keep exact in-memory compatibility and edge-case behavior.
  - `Matrix` convention guardrail explicitly calls out preserving current layout/convention semantics when forwarding.

## 6. SphericalHarmonics warning guardrail
- CS8618 in `SphericalHarmonics.cs` is intentionally not fixed in M7b.
- Future Shine must preserve the deserialization lifecycle where internal serialization constructors may leave `Coefficients`/`baseValues` uninitialized until object materialization completes.
- Any nullable/required/initializer change must be validated against serializer behavior before applying warning cleanup.

## 7. Validation results
1) Command: `dotnet build striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Mathematics`
- Exit code: `0`
- First meaningful warning/error: `CS8618` on `SphericalHarmonics.cs` (`Coefficients` non-nullable not initialized in serialization constructor)
- Pass/fail: **PASS**
- Output truncated: **No**

2) Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: warning from transitive build (`CS8604` in `striv/projects/StriV.AssetPipeline/AssetPipeline.cs`)
- Pass/fail: **PASS**
- Output truncated: **No**

3) Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none emitted
- Pass/fail: **PASS**
- Output truncated: **No**

4) Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none emitted
- Pass/fail: **PASS**
- Output truncated: **No**

5) Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: transitive warnings in engine projects during build (e.g., `CS8765` nullability mismatch)
- Pass/fail: **PASS**
- Output truncated: **Yes** (tool output cap)

6) Command: `./striv/build/striv-build-core.sh`
- Exit code: `0`
- First meaningful warning/error: `CS1030 #warning` in `ObjectIdBuilder.cs` plus existing nullable warnings in transitive projects
- Pass/fail: **PASS**
- Output truncated: **Yes** (tool output cap)

## 8. Deferred cleanup
- Serialization attribute removal remains a dedicated proof/migration task.
- System.Numerics forwarding remains a dedicated equivalence-proof task.
- Shine warning cleanup (including `SphericalHarmonics` CS8618) remains deferred.
- Additional invariant documentation can be added later in color/volume/half types if future passes expose ambiguity during refactoring.

## 9. Recommended next step
- **M7c Shine for `Stride.Core.Mathematics`** with focused warning-lifecycle analysis (starting from `SphericalHarmonics` initialization semantics), while preserving the guardrails documented in M7b.
