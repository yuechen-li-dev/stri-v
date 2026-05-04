# Stri-V Shader Pipeline M4b Validation

## 1) Files changed
- striv/StriV.Core.slnx
- striv/projects/StriV.ShaderPipeline/StriV.ShaderPipeline.csproj
- striv/projects/StriV.ShaderPipeline/Lexing/SourceSpan.cs
- striv/projects/StriV.ShaderPipeline/Lexing/TokenKind.cs
- striv/projects/StriV.ShaderPipeline/Lexing/Token.cs
- striv/projects/StriV.ShaderPipeline/Lexing/ShaderLexer.cs
- striv/projects/StriV.ShaderPipeline/Diagnostics/Diagnostic.cs
- striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs
- striv/projects/StriV.ShaderPipeline/Parsing/ParseResult.cs
- striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs
- striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs
- striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs
- striv/tests/fixtures/shaders/plain/simple_vertex_pixel.hlsl
- striv/tests/fixtures/shaders/sdsl/simple_stream_shader.sdsl

## 2) Project design
- Project path: `striv/projects/StriV.ShaderPipeline/StriV.ShaderPipeline.csproj`
- Test project path: `striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- Dependency policy: standalone SDK-style net10.0 class library; tests reference pipeline project + xUnit only.
- Confirmed exclusions: no ANTLR/parser generator packages, no legacy Stride shader compiler/parser references, no CppNet references.

## 3) Lexer/parser design
- Token kinds: Identifier, Keyword, NumericLiteral, StringLiteral, Punctuation, Operator, Comment, PreprocessorDirective, EndOfFile.
- AST nodes: `HlslDocument`, `HlslFunction`, `SdslShader`, `SdslStream`, `SdslStageMethod`.
- Parser subset:
  - HLSL shallow function signature capture + balanced body capture.
  - SDSL `shader Name`, `stage stream`, `stage override` with raw balanced method body.
- Raw body preservation: method/function bodies are extracted as balanced text blocks and held as raw text.
- Diagnostics strategy: simple `Diagnostic` record + parse result container; currently reports missing shader header for SDSL and otherwise accepts subset.

## 4) Lowering design
- Streams represented as generated `struct StageStreams` plus `static StageStreams __streams;`.
- `streams.X` rewritten using text substitution to `__streams.X` within preserved method bodies.
- Intentionally not handled: semantic/type validation, full HLSL generation correctness, mixins/base/clone/compose/partial effect, generics specialization.

## 5) Fixtures
- `plain/simple_vertex_pixel.hlsl`: baseline plain HLSL structs + VS/PS entry points.
- `sdsl/simple_stream_shader.sdsl`: minimal SDSL shader with stream declarations and stage overrides using `streams.*`.

## 6) Test results
1. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - exit code: 0
   - first meaningful warning/error: none
   - pass/fail: PASS (5 tests)
   - output truncated: no
2. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - exit code: 0
   - first meaningful warning/error: none shown
   - pass/fail: PASS
   - output truncated: no
3. `./striv/build/striv-build-core.sh`
   - exit code: 0
   - first meaningful warning/error: existing upstream warnings (nullability/#warning) during core build, no errors
   - pass/fail: PASS
   - output truncated: yes (tool output truncated due size)

## 7) Limitations
- Not a full HLSL parser.
- Not full SDSL.
- No mixin merge.
- No clone.
- No base/override resolution yet.
- No partial effect support yet.
- No SPIR-V emission yet.
- DXC optional only and not required in this milestone.

## 8) Recommended next task
- Implement stream weaving more formally (typed stage inputs/outputs + deterministic mapping) before expanding advanced SDSL constructs.
