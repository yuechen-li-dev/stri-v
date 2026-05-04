# 680 — StriV Shader Pipeline M4g SpriteBatch Fixture Validation

## 1) Files changed
- `striv/tests/fixtures/shaders/sdsl/SpriteBatchShader.sdsl`
- `striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs`
- `striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs`
- `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs`

## 2) Fixture choice
- Fixture strategy: **copied exactly** from `sources/engine/Stride.Graphics/Shaders/SpriteBatchShader.sdsl` into test fixtures.
- Reason this fixture is next after simple stream shader: it introduces first “real-ish” production SDSL constructs while still fitting current handwritten parser/lowerer boundaries.
- Features exercised:
  - shader generics (`<bool TSRgb>`)
  - base inheritance (`: SpriteBase`)
  - stage streams
  - stage overrides (`VSMain`, `Shading`)
  - `streams.*` usage
  - `base.*` calls

## 3) Parser changes
- Header parsing now captures:
  - shader name
  - optional generic parameter raw text
  - optional base shader list (comma-separated)
- Parser emits structured known-unsupported diagnostics when generics/inheritance are present.
- Stage method/body parsing remains raw-body preservation (no expression parser added).
- Comments and pre-existing content remain parse-tolerated via regex scanning and balanced-block extraction.

## 4) Diagnostics added
- `SD300`: shader inheritance parsed but mixin merge is not implemented.
- `SD301`: generic parameters parsed but specialization is not implemented.
- `SD302`: base call detected but base resolution is not implemented.

## 5) Lowering behavior
- Lowering now emits deterministic TODO comments in lowered HLSL when shader-level unsupported semantics are present:
  - TODO for generics (`SD301`)
  - TODO for inheritance (`SD300`)
- Lowering emits `SD302` diagnostics when raw method bodies contain `base.` calls.
- Raw stage method bodies are preserved in emitted methods (unchanged strategy).
- DXC compile for SpriteBatchShader is intentionally **not required** at M4g because inheritance/base call resolution and generic specialization are intentionally out of scope.

## 6) Test results
1. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **no**

2. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **no**

3. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **no**

## 7) Limitations
- no inheritance/mixin merge yet
- no base resolution yet
- no generic specialization yet
- no `clone`
- no partial effect
- no SpriteBatchShader SPIR-V output yet

## 8) Recommended next task
- **Implement base-call diagnostic/lowering placeholder improvements**:
  - include method name and count of detected `base.*` invocations
  - emit targeted TODO comments near affected methods
  - keep behavior deterministic while inheritance design is pending
