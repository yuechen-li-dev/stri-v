# Stri-V Shader Pipeline M4h Base-Call Validation

## 1) Files changed
- `striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs`
- `striv/projects/StriV.ShaderPipeline/Parsing/BaseCallScanner.cs`
- `striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs`
- `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs`

## 2) Base-call model
- Added `BaseCall` AST node with `MethodName`, `ArgumentText`, `ArgumentCount`, and `Span`.
- Added `IReadOnlyList<BaseCall> BaseCalls` to `SdslStageMethod`.
- Base calls are detected during SDSL method parse, using a handwritten body scanner over raw method body text.
- Source position strategy:
  - scanner records call-relative start offset in method body,
  - then computes line/column by scanning from method span,
  - produces deterministic `SourceSpan` for each detected call.

## 3) Scanner behavior
- Supports whitespace variants: `base.VSMain()`, `base . VSMain ( )`.
- Captures balanced argument text between `(` and matching `)` (including nested parens/brackets/braces).
- Counts top-level comma-separated arguments.
- Ignores `base.` inside:
  - line comments `// ...`
  - block comments `/* ... */`
  - double-quoted strings with escaped characters
- Known limitations:
  - no semantic expression parse,
  - no preprocessor-awareness,
  - does not attempt to recover malformed unmatched calls beyond skipping.

## 4) Diagnostics
- `SD302` now includes:
  - base method preview (`base.Method()` or `base.Method(...)`),
  - argument count,
  - containing stage method name,
  - source line/column from `BaseCall.Span`.
- Message shape:
  - `Base call 'base.<Method>(...)' with N argument(s) in stage method '<StageMethod>' cannot be resolved until mixin merge is implemented.`
- `SD302` remains non-fatal.
- Emission point: lowering (`ShaderLowerer.LowerSdslToHlsl`) per detected `BaseCall`.

## 5) Lowering placeholder behavior
- Lowering now emits targeted TODO comments near method body top:
  - `// TODO SD302: unresolved base call base.<Method>(...) with N argument(s) in <StageMethod>; mixin merge not implemented.`
- Raw method body text is preserved and still emitted.
- No base resolution implemented yet by design (mixin merge/inheritance intentionally deferred).

## 6) Tests
Added/updated tests in `StriV.ShaderPipeline.Tests`:
- `BaseCallScanner_DetectsSimpleBaseCalls`
  - proves method name and zero-arg detection.
- `BaseCallScanner_DetectsArguments`
  - proves argument text capture and top-level arg count.
- `BaseCallScanner_IgnoresCommentsAndStrings`
  - proves false positives in comments/strings are ignored.
- `SpriteBatchShader_Parse_CapturesBaseCalls`
  - proves fixture parsing records base-call inventory.
- `SpriteBatchShader_Lowering_EmitsTargetedBaseCallTodo`
  - proves lowered output includes SD302 TODO + method/call context.
- Existing SpriteBatch tests updated to assert modeled base calls/targeted diagnostics.

## 7) Validation results
1. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

2. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

3. Command: `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: none (`0 Warning(s)` in captured log)
   - Pass/fail: PASS
   - Output truncated: no (captured to log)

## 8) Limitations
- no mixin merge yet.
- no inheritance resolution yet.
- no clone.
- no full HLSL expression parsing.
- base calls are inventoried/diagnosed only.
- SpriteBatchShader output is not expected to compile yet.

## 9) Recommended next task
**Recommendation:** inheritance/mixin merge design.

Reason: base-call inventory and source-located diagnostics are now available and deterministic; the next highest-value step is to define merge/resolution semantics that can consume this model without expanding into full expression parsing.
