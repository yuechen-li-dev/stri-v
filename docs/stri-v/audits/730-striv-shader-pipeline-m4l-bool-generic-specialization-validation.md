# 730 - StriV Shader Pipeline M4l Bool Generic Specialization Validation

## 1. Files changed
- striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs
- striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs
- striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs
- striv/tests/StriV.ShaderPipeline.Tests/GenericSpecializationTests.cs
- striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchPairTests.cs
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs

## 2. Generic model
- Added `ShaderGenericParameter` in AST with `TypeText`, `Name`, and `Span`.
- `SdslShader` now carries both raw generic text (`GenericParametersText`) and parsed parameters (`GenericParameters`).
- Parser recognizes comma-separated `type name` entries and currently supports downstream specialization only for `bool` type.
- Failed parse emits `SD323` while preserving raw generic text.

## 3. Specialization behavior
- Lowering now accepts optional `ShaderSpecialization` containing bool map (`parameter -> value`).
- Supported `bool` parameters map to lowercase HLSL literals (`true`/`false`).
- Diagnostics:
  - `SD320`: missing required specialization value.
  - `SD321`: unsupported generic parameter type.
  - `SD322`: specialization provided for unknown parameter.
  - `SD323`: failed parse of generic parameter list.

## 4. Identifier substitution
- Added token-aware scanner for substitution that replaces identifier tokens only.
- Does not replace inside longer identifiers (e.g. `TSRgb2`).
- Skips string literals, line comments (`//`), and block comments (`/* */`).
- Limitation: scanner is lightweight and not a full lexer/parser; it intentionally targets current fixture needs.

## 5. SpriteBatch behavior
- With `TSRgb=false`, lowered output removes standalone `TSRgb` code identifiers and emits `false` in their place.
- With `TSRgb=true`, lowered output emits `true` substitutions.
- Supported bool specialization path no longer depends on generic TODO diagnostic for this case.
- Base/inheritance diagnostics still behave independently for unsupported semantics.
- DXC compile for SpriteBatch remains out of scope for this milestone; only specialization/lowering behavior is validated.

## 6. Tests
Added `GenericSpecializationTests`:
- `GenericParser_ParsesBoolParameter`: verifies parsed AST generic parameter shape.
- `GenericSpecialization_ReplacesStandaloneBoolIdentifier`: verifies token-safe substitution and string/comment protection.
- `SpriteBatchSpecialization_False_RemovesUnsupportedGenericDiagnostic`: verifies `TSRgb=false` specialization path and no standalone `TSRgb` in output.
- `SpriteBatchSpecialization_True_ReplacesWithTrue`: verifies `true` substitution path.
- `SpriteBatchSpecialization_MissingValue_DiagnosesMissingSpecialization`: verifies `SD320`.
- `Specialization_UnknownKey_DiagnosesUnknownParameter`: verifies `SD322`.

Updated existing tests to align with parsed-generic behavior without `SD301` dependency.

## 7. Validation results
1) `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- Exit code: 0
- First meaningful warning/error: xUnit analyzer warnings in existing tests (`xUnit2029`, `xUnit2031`)
- Result: PASS
- Output truncated: no

2) `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Exit code: 0
- First meaningful warning/error: none emitted
- Result: PASS
- Output truncated: no

3) `./striv/build/striv-build-core.sh`
- Exit code: 0
- First meaningful warning/error: existing repository warnings (e.g. `CS1030` / nullability warnings)
- Result: PASS
- Output truncated: yes (tool output capped)

## 8. Limitations
- Bool value parameters only.
- No type generics.
- No numeric generics.
- No expression evaluation.
- No generic base specialization.
- No clone/compose/partial effect.
- No SpriteBatch SPIR-V yet.

## 9. Recommended next task
- Improve SpriteBatch lowering toward DXC by addressing remaining unsupported semantics incrementally while preserving optional DXC gating.
