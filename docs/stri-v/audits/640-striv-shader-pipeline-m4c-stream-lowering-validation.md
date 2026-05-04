# 640 – StriV Shader Pipeline M4c Stream Lowering Validation

## 1) Files changed
- striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs
- striv/projects/StriV.ShaderPipeline/Diagnostics/Diagnostic.cs
- striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs
- striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs

## 2) Stream model design
- Added `SdslStream` span-aware representation: type/name/semantic/source span.
- Added richer `SdslStageMethod` representation: return type/name/parameters/raw body/modifiers/span.
- Added lowering-time stream model: `StreamBinding` + `StreamLayout`.
- Stream ordering is source-order preserving by iterating parsed stream list as-is.
- Duplicate detection is non-throwing and emits structured diagnostics for duplicate names and semantics.

## 3) Lowering design
- Replaced toy static carrier lowering with generated `struct StriVStageStreams` field semantics.
- `VSMain` lowers to `StriVStageStreams VSMain()` and gets a local `StriVStageStreams streams;` plus auto `return streams;`.
- `PSMain` lowers to `PSMain(StriVStageStreams streams)` and appends `: SV_Target` for `float4` return in the current subset.
- Legacy `static StageStreams __streams;` and `__streams.` rewrite removed to model explicit stage IO.
- Unsupported stage method names are emitted with TODO comments and diagnostic.
- Known limitations kept explicit with diagnostics for dropped parameters and pre-existing VS returns.

## 4) Diagnostics
- `SD000`: Missing shader header.
- `SD200`: Duplicate stream name.
- `SD201`: Duplicate stream semantic.
- `SD203`: Unsupported stage method name.
- `SD204`: Unsupported original method parameters for VSMain/PSMain lowering.
- `SD205`: Existing return in VSMain may conflict with generated return.

## 5) Test results
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
   - First meaningful warning/error: existing repository warnings (e.g., CS1030 in Stride.Core)
   - Pass/fail: PASS
   - Output truncated: yes (tool output cap)

## 6) Limitations
- No full HLSL parser.
- No mixin merge.
- No base/override resolution.
- No clone.
- No partial effect.
- Method bodies still preserved as raw balanced text.
- VS/PS identification is name-based (`VSMain` / `PSMain`).
- No runtime graphics integration.
- No guaranteed SPIR-V compile path yet.

## 7) Recommended next task
- Add DXC compile smoke for lowered simple shader (optional/skippable when DXC unavailable).
